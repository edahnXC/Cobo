using System.Text.Json;
using CollaborativeWorkspace.Application.Interfaces;
using CollaborativeWorkspace.Domain.Entities;
using CollaborativeWorkspace.Domain.Enums;
using CollaborativeWorkspace.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CollaborativeWorkspace.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly bool _isSandbox;

    public EmailService(AppDbContext db, IConfiguration config, ILogger<EmailService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _isSandbox = bool.TryParse(config["SendGrid:SandboxMode"], out var s) && s;
    }

    public async Task SendAsync(string templateKey, string toEmail, Dictionary<string, string> placeholders, Guid? relatedUserId = null)
    {
        // 1. Load template
        var template = _db.EmailTemplates
            .FirstOrDefault(t => t.TemplateKey == templateKey && t.IsActive)
            ?? throw new InvalidOperationException($"Email template '{templateKey}' not found or inactive.");

        // 2. Replace placeholders
        var subject = ReplacePlaceholders(template.Subject, placeholders);
        var htmlBody = ReplacePlaceholders(template.HtmlBody, placeholders);

        // 3. Create email log entry
        var log = new EmailLog
        {
            LogId = Guid.NewGuid(),
            TemplateKey = templateKey,
            ToEmail = toEmail,
            Subject = subject,
            HtmlBody = htmlBody,
            Status = EmailStatus.Pending.ToString(),
            RelatedUserId = relatedUserId,
            CreatedAt = DateTime.UtcNow
        };
        _db.EmailLogs.Add(log);
        await _db.SaveChangesAsync();

        // In sandbox/dev mode: skip SendGrid API call entirely, just log to DB
        if (_isSandbox)
        {
            _logger.LogInformation("[SANDBOX] Email skipped — To: {Email}, Subject: {Subject}", toEmail, subject);
            log.Status = EmailStatus.Sent.ToString();
            log.SentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return;
        }

        try
        {
            var apiKey = _config["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid:ApiKey not configured.");
            var fromEmail = _config["SendGrid:FromEmail"] ?? "noreply@cobo.app";
            var fromName = _config["SendGrid:FromName"] ?? "Cobo Platform";

            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage
            {
                From = new EmailAddress(fromEmail, fromName),
                Subject = subject,
                HtmlContent = htmlBody,
                PlainTextContent = template.PlainBody is not null ? ReplacePlaceholders(template.PlainBody, placeholders) : null
            };
            msg.AddTo(new EmailAddress(toEmail));

            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
            {
                log.Status = EmailStatus.Sent.ToString();
                log.SentAt = DateTime.UtcNow;
            }
            else
            {
                var body = await response.Body.ReadAsStringAsync();
                log.Status = EmailStatus.Failed.ToString();
                log.FailedAt = DateTime.UtcNow;
                log.ErrorMessage = $"HTTP {(int)response.StatusCode}: {body}";
                log.RetryCount++;
                _logger.LogError("SendGrid failed for {Email}: {Error}", toEmail, log.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            log.Status = EmailStatus.Failed.ToString();
            log.FailedAt = DateTime.UtcNow;
            log.ErrorMessage = ex.Message;
            log.RetryCount++;
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
        finally
        {
            await _db.SaveChangesAsync();
        }
    }

    private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        foreach (var (key, value) in placeholders)
            template = template.Replace($"{{{{{key}}}}}", value);
        return template;
    }
}
