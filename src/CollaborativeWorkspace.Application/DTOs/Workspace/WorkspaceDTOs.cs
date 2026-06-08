namespace CollaborativeWorkspace.Application.DTOs.Workspace;

// ─── Requests ─────────────────────────────────────────────────────────────────

public record CreateWorkspaceRequest(
    string Name,
    string? Description,
    string? LogoUrl
);

public record UpdateWorkspaceRequest(
    string? Name,
    string? Description,
    string? LogoUrl
);

public record InviteMemberRequest(
    string Email
);

public record UpdateMemberRoleRequest(
    string Role  // Owner | Moderator | Member | PremiumMember
);

// ─── Responses ────────────────────────────────────────────────────────────────

public record WorkspaceResponse(
    Guid WorkspaceId,
    string Name,
    string? Description,
    string? LogoUrl,
    bool IsActive,
    Guid OwnerId,
    string OwnerName,
    int MemberCount,
    DateTime CreatedAt
);

public record WorkspaceMemberResponse(
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl,
    string MemberRole,
    DateTime JoinedAt
);

public record WorkspaceInvitationResponse(
    Guid InvitationId,
    Guid WorkspaceId,
    string WorkspaceName,
    string InvitedEmail,
    string InvitedByName,
    string Status,
    DateTime ExpiresAt,
    DateTime CreatedAt
);
