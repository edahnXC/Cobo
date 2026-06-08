namespace CollaborativeWorkspace.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(string templateKey, string toEmail, Dictionary<string, string> placeholders, Guid? relatedUserId = null);
}
