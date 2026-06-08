using CollaborativeWorkspace.Domain.Enums;

namespace CollaborativeWorkspace.Domain.Entities;

public class Channel
{
    public Guid ChannelId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelType { get; set; } = Enums.ChannelType.Text.ToString();
    public bool IsPrivate { get; set; } = false;
    public bool IsPremium { get; set; } = false;
    public bool IsArchived { get; set; } = false;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Workspace Workspace { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<ChannelMember> ChannelMembers { get; set; } = new List<ChannelMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<CallRoom> CallRooms { get; set; } = new List<CallRoom>();
    public ICollection<GameRoom> GameRooms { get; set; } = new List<GameRoom>();
}

public class ChannelMember
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public Guid UserId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public Channel Channel { get; set; } = null!;
    public User User { get; set; } = null!;
}
