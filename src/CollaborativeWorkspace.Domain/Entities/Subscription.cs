using CollaborativeWorkspace.Domain.Enums;

namespace CollaborativeWorkspace.Domain.Entities;

public class Plan
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty; // Free | Pro | Enterprise
    public decimal Price { get; set; } = 0.00m;
    public int DurationDays { get; set; }
    public int? MaxMembers { get; set; } // null = unlimited
    public string? StripePriceId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Features { get; set; } // JSON array

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

public class Subscription
{
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public int PlanId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = SubscriptionStatus.Active.ToString();
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
}

public class StripeEvent
{
    public Guid EventId { get; set; }
    public string StripeEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; } = false;
    public string? Error { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
