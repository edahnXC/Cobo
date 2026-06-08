namespace CollaborativeWorkspace.Application.DTOs.Complaint;

// ─── Requests ─────────────────────────────────────────────────────────────────

public record SubmitComplaintRequest(
    Guid? ReportedUserId,
    Guid? WorkspaceId,
    Guid? MessageId,
    string Category,   // Harassment | Spam | InappropriateContent | TechnicalIssue | Other
    string Description
);

public record ResolveComplaintRequest(
    string Status,     // Resolved | Dismissed | UnderReview
    string? Resolution
);

// ─── Responses ────────────────────────────────────────────────────────────────

public record ComplaintResponse(
    Guid ComplaintId,
    Guid ReportedByUserId,
    string ReportedByName,
    Guid? ReportedUserId,
    string? ReportedUserName,
    Guid? WorkspaceId,
    Guid? MessageId,
    string Category,
    string Description,
    string Status,
    string? Resolution,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
