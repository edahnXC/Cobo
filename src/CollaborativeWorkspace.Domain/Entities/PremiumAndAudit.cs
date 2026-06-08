namespace CollaborativeWorkspace.Domain.Entities;

public class CallRoom
{
    public Guid CallRoomId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid CreatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EndedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Workspace Workspace { get; set; } = null!;
    public Channel Channel { get; set; } = null!;
    public User Creator { get; set; } = null!;
}

public class GameRoom
{
    public Guid GameRoomId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ChannelId { get; set; }
    public string GameType { get; set; } = "Scribble";
    public bool IsActive { get; set; } = true;
    public DateTime? EndedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Workspace Workspace { get; set; } = null!;
    public Channel Channel { get; set; } = null!;
}

public class AuditLog
{
    public long LogId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
