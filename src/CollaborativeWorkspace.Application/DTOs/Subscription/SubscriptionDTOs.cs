namespace CollaborativeWorkspace.Application.DTOs.Subscription;

// ─── Requests ─────────────────────────────────────────────────────────────────

public record CreateCheckoutRequest(
    int PlanId
);

// ─── Responses ────────────────────────────────────────────────────────────────

public record PlanResponse(
    int PlanId,
    string PlanName,
    decimal Price,
    string Currency,
    int DurationDays,
    int? MaxMembers,
    string? StripePriceId,
    IEnumerable<string> Features
);

public record SubscriptionResponse(
    Guid SubscriptionId,
    int PlanId,
    string PlanName,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive
);

public record CheckoutResponse(
    string CheckoutUrl,
    string SessionId
);
