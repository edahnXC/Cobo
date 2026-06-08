using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Subscription;
using CollaborativeWorkspace.Application.Interfaces;
using CollaborativeWorkspace.Domain.Entities;
using CollaborativeWorkspace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace CollaborativeWorkspace.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(AppDbContext db, IEmailService email, IAuditService audit, IConfiguration config, ILogger<SubscriptionService> logger)
    {
        _db = db;
        _email = email;
        _audit = audit;
        _config = config;
        _logger = logger;

        StripeConfiguration.ApiKey = config["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey not configured.");
    }

    public async Task<ServiceResult<IEnumerable<PlanResponse>>> GetPlansAsync()
    {
        var plans = await _db.Plans.Where(p => p.IsActive).ToListAsync();
        return ServiceResult<IEnumerable<PlanResponse>>.Success(plans.Select(MapPlan));
    }

    public async Task<ServiceResult<SubscriptionResponse?>> GetMySubscriptionAsync(Guid userId)
    {
        var sub = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.Status == "Active" && s.EndDate > DateTime.UtcNow)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        return ServiceResult<SubscriptionResponse?>.Success(sub is null ? null : MapSubscription(sub));
    }

    public async Task<ServiceResult<CheckoutResponse>> CreateCheckoutSessionAsync(Guid userId, CreateCheckoutRequest request)
    {
        var plan = await _db.Plans.FindAsync(request.PlanId);
        if (plan is null || !plan.IsActive)
            return ServiceResult<CheckoutResponse>.NotFound("Plan not found or inactive.");

        if (string.IsNullOrWhiteSpace(plan.StripePriceId))
            return ServiceResult<CheckoutResponse>.Failure("This plan does not have Stripe pricing configured.");

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return ServiceResult<CheckoutResponse>.NotFound("User not found.");

        // Ensure Stripe customer exists
        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = user.Email,
                Name = user.FullName,
                Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString() }
            });
            user.StripeCustomerId = customer.Id;
            await _db.SaveChangesAsync();
        }

        var baseUrl = _config["AppSettings:BaseUrl"] ?? "https://localhost:4200";
        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Customer = user.StripeCustomerId,
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new() { Price = plan.StripePriceId, Quantity = 1 }
            },
            SuccessUrl = $"{baseUrl}/subscription/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}/plans",
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.ToString(),
                ["planId"] = plan.PlanId.ToString()
            },
            Currency = "inr"
        });

        await _audit.LogAsync(userId, "STRIPE_CHECKOUT_CREATED", "Plan", plan.PlanId.ToString(), new { session.Id });
        return ServiceResult<CheckoutResponse>.Success(new CheckoutResponse(session.Url, session.Id));
    }

    public async Task<ServiceResult<string>> CancelSubscriptionAsync(Guid userId)
    {
        var sub = await _db.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active" && s.EndDate > DateTime.UtcNow);

        if (sub is null)
            return ServiceResult<string>.Failure("No active subscription found.");

                if (!string.IsNullOrWhiteSpace(sub.StripeSubscriptionId))
        {
            var stripeSubService = new Stripe.SubscriptionService();
            await stripeSubService.CancelAsync(sub.StripeSubscriptionId, new SubscriptionCancelOptions());
        }

        sub.Status = "Cancelled";
        sub.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var user = await _db.Users.FindAsync(userId);
        await _email.SendAsync("subscription_cancelled", user!.Email, new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["PlanName"] = sub.Plan.PlanName
        }, userId);

        await _audit.LogAsync(userId, "SUBSCRIPTION_CANCEL", "Subscription", sub.SubscriptionId.ToString());
        return ServiceResult<string>.Success("Subscription cancelled.");
    }

    public async Task HandleStripeWebhookAsync(string json, string stripeSignature)
    {
        var webhookSecret = _config["Stripe:WebhookSecret"]
            ?? throw new InvalidOperationException("Stripe:WebhookSecret not configured.");

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed.");
            throw;
        }

        // Idempotency check
        var alreadyProcessed = await _db.StripeEvents
            .AnyAsync(e => e.StripeEventId == stripeEvent.Id && e.IsProcessed);
        if (alreadyProcessed)
        {
            _logger.LogInformation("Stripe event {EventId} already processed — skipping.", stripeEvent.Id);
            return;
        }

        var eventLog = new StripeEvent
        {
            EventId = Guid.NewGuid(),
            StripeEventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            Payload = json,
            ReceivedAt = DateTime.UtcNow
        };
        _db.StripeEvents.Add(eventLog);
        await _db.SaveChangesAsync();

        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutCompleted(stripeEvent);
                    break;
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;
                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;
                case "invoice.payment_failed":
                    await HandlePaymentFailed(stripeEvent);
                    break;
                case "invoice.payment_succeeded":
                    await HandlePaymentSucceeded(stripeEvent);
                    break;
                default:
                    _logger.LogInformation("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                    break;
            }

            eventLog.IsProcessed = true;
            eventLog.ProcessedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            eventLog.Error = ex.Message;
            _logger.LogError(ex, "Error processing Stripe event {EventId}", stripeEvent.Id);
        }
        finally
        {
            await _db.SaveChangesAsync();
        }
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session is null) return;

        if (!session.Metadata.TryGetValue("userId", out var userIdStr) ||
            !session.Metadata.TryGetValue("planId", out var planIdStr)) return;

        var userId = Guid.Parse(userIdStr);
        var planId = int.Parse(planIdStr);

        var plan = await _db.Plans.FindAsync(planId);
        var user = await _db.Users.FindAsync(userId);
        if (plan is null || user is null) return;

        var subscription = new Domain.Entities.Subscription
        {
            SubscriptionId = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            StripeSubscriptionId = session.SubscriptionId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(plan.DurationDays),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        _db.Subscriptions.Add(subscription);

        // Assign PremiumMember role if not already assigned
        var hasPremiumRole = await _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == 5);
        if (!hasPremiumRole)
            _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = 5 });

        await _db.SaveChangesAsync();

        await _email.SendAsync("subscription_activated", user.Email, new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["PlanName"] = plan.PlanName,
            ["EndDate"] = subscription.EndDate.ToString("MMMM dd, yyyy")
        }, userId);

        await _audit.LogAsync(userId, "STRIPE_PAYMENT", "Subscription", subscription.SubscriptionId.ToString(), new { plan.PlanName });
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var stripeSub = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSub is null) return;

        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);
        if (sub is null) return;

        sub.EndDate = stripeSub.CurrentPeriodEnd;
        sub.Status = stripeSub.Status switch
        {
            "active" => "Active",
            "past_due" => "PastDue",
            "canceled" => "Cancelled",
            _ => sub.Status
        };
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var stripeSub = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSub is null) return;

        var sub = await _db.Subscriptions.Include(s => s.User).Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);
        if (sub is null) return;

        sub.Status = "Cancelled";
        sub.CancelledAt = DateTime.UtcNow;

        // Remove PremiumMember role
        var premiumRole = await _db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == sub.UserId && ur.RoleId == 5);
        if (premiumRole is not null) _db.UserRoles.Remove(premiumRole);

        if (sub.User is not null)
        {
            await _email.SendAsync("subscription_cancelled", sub.User.Email, new Dictionary<string, string>
            {
                ["FullName"] = sub.User.FullName,
                ["PlanName"] = sub.Plan?.PlanName ?? "your plan"
            }, sub.UserId);
        }
    }

    private async Task HandlePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice?.SubscriptionId is null) return;

        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == invoice.SubscriptionId);
        if (sub is not null)
            sub.Status = "PastDue";
    }

    private async Task HandlePaymentSucceeded(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice?.SubscriptionId is null) return;

        var sub = await _db.Subscriptions.Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == invoice.SubscriptionId);
        if (sub is not null)
        {
            sub.Status = "Active";
            sub.EndDate = DateTime.UtcNow.AddDays(sub.Plan?.DurationDays ?? 30);
        }
    }

    private static PlanResponse MapPlan(Domain.Entities.Plan p)
    {
        var features = string.IsNullOrWhiteSpace(p.Features)
            ? Enumerable.Empty<string>()
            : System.Text.Json.JsonSerializer.Deserialize<IEnumerable<string>>(p.Features) ?? Enumerable.Empty<string>();

        return new PlanResponse(p.PlanId, p.PlanName, p.Price, "INR", p.DurationDays, p.MaxMembers, p.StripePriceId, features);
    }

    private static SubscriptionResponse MapSubscription(Domain.Entities.Subscription s) =>
        new(s.SubscriptionId, s.PlanId, s.Plan?.PlanName ?? string.Empty, s.Status, s.StartDate, s.EndDate, s.Status == "Active" && s.EndDate > DateTime.UtcNow);
}
