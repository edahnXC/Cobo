using System.Text.Json;
using CollaborativeWorkspace.Application.Interfaces;
using CollaborativeWorkspace.Domain.Entities;
using CollaborativeWorkspace.Infrastructure.Data;

namespace CollaborativeWorkspace.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(Guid? userId, string action, string? entityType = null, string? entityId = null, object? details = null, string? ipAddress = null, string? userAgent = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details is not null ? JsonSerializer.Serialize(details) : null,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
