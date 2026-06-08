namespace CollaborativeWorkspace.Application.DTOs.Admin;

public record AdminUserResponse(
    Guid UserId,
    string FullName,
    string Email,
    bool IsEmailVerified,
    bool IsActive,
    string? AvatarUrl,
    IEnumerable<string> Roles,
    string? ActivePlan,
    DateTime CreatedAt
);

public record UpdateUserStatusRequest(
    bool IsActive
);

public record AuditLogResponse(
    long LogId,
    Guid? UserId,
    string? UserEmail,
    string Action,
    string? EntityType,
    string? EntityId,
    string? Details,
    string? IpAddress,
    DateTime CreatedAt
);

public record EmailLogResponse(
    Guid LogId,
    string? TemplateKey,
    string ToEmail,
    string Subject,
    string Status,
    DateTime? SentAt,
    string? ErrorMessage,
    int RetryCount,
    DateTime CreatedAt
);
