using CollaborativeWorkspace.Domain.Enums;

namespace CollaborativeWorkspace.Domain.Entities;

public class Workspace
{
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    public ICollection<WorkspaceInvitation> Invitations { get; set; } = new List<WorkspaceInvitation>();
    public ICollection<Channel> Channels { get; set; } = new List<Channel>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}

public class WorkspaceMember
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string MemberRole { get; set; } = "Member";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public Workspace Workspace { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class WorkspaceInvitation
{
    public Guid InvitationId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Status { get; set; } = InvitationStatus.Pending.ToString();
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public Workspace Workspace { get; set; } = null!;
    public User InvitedBy { get; set; } = null!;
}
