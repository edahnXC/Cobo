namespace CollaborativeWorkspace.Domain.Entities;

public class UserSession
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTime LoginAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime? LogoutAt { get; set; }
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
}
