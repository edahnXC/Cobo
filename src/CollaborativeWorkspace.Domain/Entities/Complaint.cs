using CollaborativeWorkspace.Domain.Enums;

namespace CollaborativeWorkspace.Domain.Entities;

public class Complaint
{
    public Guid ComplaintId { get; set; }
    public Guid ReportedByUserId { get; set; }
    public Guid? ReportedUserId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public Guid? MessageId { get; set; }
    public string Category { get; set; } = ComplaintCategory.Other.ToString();
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = ComplaintStatus.Open.ToString();
    public Guid? ResolvedByUserId { get; set; }
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User ReportedBy { get; set; } = null!;
    public User? ReportedUser { get; set; }
    public Workspace? Workspace { get; set; }
    public Message? Message { get; set; }
    public User? ResolvedBy { get; set; }
}
