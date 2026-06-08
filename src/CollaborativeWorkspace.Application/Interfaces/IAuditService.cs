namespace CollaborativeWorkspace.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(Guid? userId, string action, string? entityType = null, string? entityId = null, object? details = null, string? ipAddress = null, string? userAgent = null);
}
