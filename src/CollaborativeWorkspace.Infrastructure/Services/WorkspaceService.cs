using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Workspace;
using CollaborativeWorkspace.Application.Interfaces;
using CollaborativeWorkspace.Domain.Entities;
using CollaborativeWorkspace.Domain.Enums;
using CollaborativeWorkspace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CollaborativeWorkspace.Infrastructure.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;

    public WorkspaceService(AppDbContext db, IEmailService email, IAuditService audit, IConfiguration config)
    {
        _db = db;
        _email = email;
        _audit = audit;
        _config = config;
    }

    public async Task<ServiceResult<IEnumerable<WorkspaceResponse>>> GetMyWorkspacesAsync(Guid userId)
    {
        var workspaces = await _db.WorkspaceMembers
            .Where(wm => wm.UserId == userId && wm.IsActive)
            .Include(wm => wm.Workspace).ThenInclude(w => w.Owner)
            .Include(wm => wm.Workspace).ThenInclude(w => w.Members)
            .Select(wm => wm.Workspace)
            .Where(w => w.IsActive)
            .ToListAsync();

        return ServiceResult<IEnumerable<WorkspaceResponse>>.Success(workspaces.Select(MapWorkspace));
    }

    public async Task<ServiceResult<WorkspaceResponse>> CreateWorkspaceAsync(Guid userId, CreateWorkspaceRequest request)
    {
        var workspace = new Workspace
        {
            WorkspaceId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            OwnerId = userId,
            LogoUrl = request.LogoUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Workspaces.Add(workspace);

        // Add creator as Owner member
        _db.WorkspaceMembers.Add(new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.WorkspaceId,
            UserId = userId,
            MemberRole = MemberRole.Owner.ToString(),
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });

        // Create a default #general channel
        _db.Channels.Add(new Channel
        {
            ChannelId = Guid.NewGuid(),
            WorkspaceId = workspace.WorkspaceId,
            ChannelName = "general",
            ChannelType = "text",
            IsPrivate = false,
            IsPremium = false,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "WORKSPACE_CREATE", "Workspace", workspace.WorkspaceId.ToString(), new { workspace.Name });

        var created = await _db.Workspaces.Include(w => w.Owner).Include(w => w.Members)
            .FirstAsync(w => w.WorkspaceId == workspace.WorkspaceId);
        return ServiceResult<WorkspaceResponse>.Success(MapWorkspace(created), 201);
    }

    public async Task<ServiceResult<WorkspaceResponse>> GetWorkspaceAsync(Guid workspaceId, Guid userId)
    {
        var workspace = await _db.Workspaces
            .Include(w => w.Owner)
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId && w.IsActive);

        if (workspace is null) return ServiceResult<WorkspaceResponse>.NotFound("Workspace not found.");

        var isMember = workspace.Members.Any(m => m.UserId == userId && m.IsActive);
        if (!isMember) return ServiceResult<WorkspaceResponse>.Forbidden("You are not a member of this workspace.");

        return ServiceResult<WorkspaceResponse>.Success(MapWorkspace(workspace));
    }

    public async Task<ServiceResult<WorkspaceResponse>> UpdateWorkspaceAsync(Guid workspaceId, Guid userId, UpdateWorkspaceRequest request)
    {
        var workspace = await _db.Workspaces.Include(w => w.Owner).Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId && w.IsActive);

        if (workspace is null) return ServiceResult<WorkspaceResponse>.NotFound("Workspace not found.");
        if (workspace.OwnerId != userId) return ServiceResult<WorkspaceResponse>.Forbidden("Only the workspace owner can update it.");

        if (request.Name is not null) workspace.Name = request.Name.Trim();
        if (request.Description is not null) workspace.Description = request.Description.Trim();
        if (request.LogoUrl is not null) workspace.LogoUrl = request.LogoUrl;
        workspace.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "WORKSPACE_UPDATE", "Workspace", workspaceId.ToString());
        return ServiceResult<WorkspaceResponse>.Success(MapWorkspace(workspace));
    }

    public async Task<ServiceResult<string>> DeleteWorkspaceAsync(Guid workspaceId, Guid userId)
    {
        var workspace = await _db.Workspaces.FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId && w.IsActive);
        if (workspace is null) return ServiceResult<string>.NotFound("Workspace not found.");
        if (workspace.OwnerId != userId) return ServiceResult<string>.Forbidden("Only the workspace owner can delete it.");

        workspace.IsActive = false;
        workspace.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "WORKSPACE_DELETE", "Workspace", workspaceId.ToString());
        return ServiceResult<string>.Success("Workspace deleted.");
    }

    public async Task<ServiceResult<IEnumerable<WorkspaceMemberResponse>>> GetMembersAsync(Guid workspaceId, Guid requesterId)
    {
        var isMember = await _db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == requesterId && m.IsActive);
        if (!isMember) return ServiceResult<IEnumerable<WorkspaceMemberResponse>>.Forbidden("Not a member.");

        var members = await _db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.IsActive)
            .Include(m => m.User)
            .ToListAsync();

        return ServiceResult<IEnumerable<WorkspaceMemberResponse>>.Success(members.Select(m =>
            new WorkspaceMemberResponse(m.User.UserId, m.User.FullName, m.User.Email, m.User.AvatarUrl, m.MemberRole, m.JoinedAt)));
    }

    public async Task<ServiceResult<string>> RemoveMemberAsync(Guid workspaceId, Guid targetUserId, Guid requesterId)
    {
        var requesterMember = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == requesterId && m.IsActive);

        if (requesterMember is null || requesterMember.MemberRole != "Owner")
            return ServiceResult<string>.Forbidden("Only the workspace owner can remove members.");

        if (targetUserId == requesterId)
            return ServiceResult<string>.Failure("Cannot remove yourself. Transfer ownership first.", "SELF_REMOVE");

        var target = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUserId && m.IsActive);

        if (target is null) return ServiceResult<string>.NotFound("Member not found.");

        target.IsActive = false;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(requesterId, "MEMBER_REMOVE", "WorkspaceMember", targetUserId.ToString(), new { workspaceId });
        return ServiceResult<string>.Success("Member removed.");
    }

    public async Task<ServiceResult<string>> UpdateMemberRoleAsync(Guid workspaceId, Guid targetUserId, Guid requesterId, UpdateMemberRoleRequest request)
    {
        var requesterMember = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == requesterId && m.IsActive);

        if (requesterMember is null || requesterMember.MemberRole != "Owner")
            return ServiceResult<string>.Forbidden("Only the workspace owner can change roles.");

        var validRoles = new[] { "Moderator", "Member", "PremiumMember" };
        if (!validRoles.Contains(request.Role))
            return ServiceResult<string>.Failure("Invalid role. Valid: Moderator, Member, PremiumMember.");

        var target = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUserId && m.IsActive);

        if (target is null) return ServiceResult<string>.NotFound("Member not found.");

        target.MemberRole = request.Role;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(requesterId, "MEMBER_ROLE_UPDATE", "WorkspaceMember", targetUserId.ToString(), new { workspaceId, newRole = request.Role });
        return ServiceResult<string>.Success("Member role updated.");
    }

    public async Task<ServiceResult<WorkspaceInvitationResponse>> SendInvitationAsync(Guid workspaceId, Guid inviterId, InviteMemberRequest request)
    {
        var workspace = await _db.Workspaces.Include(w => w.Members).FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId && w.IsActive);
        if (workspace is null) return ServiceResult<WorkspaceInvitationResponse>.NotFound("Workspace not found.");

        var inviterMember = workspace.Members.FirstOrDefault(m => m.UserId == inviterId && m.IsActive);
        if (inviterMember is null || inviterMember.MemberRole != "Owner")
            return ServiceResult<WorkspaceInvitationResponse>.Forbidden("Only the workspace owner can send invitations.");

        // Check if email is already a member
        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());
        if (targetUser is not null && workspace.Members.Any(m => m.UserId == targetUser.UserId && m.IsActive))
            return ServiceResult<WorkspaceInvitationResponse>.Failure("This user is already a member of the workspace.");

        // Check for existing pending invite
        var existing = await _db.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.InvitedEmail == request.Email.ToLower() && i.Status == "Pending" && i.ExpiresAt > DateTime.UtcNow);
        if (existing is not null)
            return ServiceResult<WorkspaceInvitationResponse>.Failure("A pending invitation already exists for this email.");

        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var invitation = new WorkspaceInvitation
        {
            InvitationId = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            InvitedByUserId = inviterId,
            InvitedEmail = request.Email.ToLower(),
            Token = token,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.AddHours(72),
            CreatedAt = DateTime.UtcNow
        };
        _db.WorkspaceInvitations.Add(invitation);
        await _db.SaveChangesAsync();

        var inviter = await _db.Users.FindAsync(inviterId);
        var baseUrl = _config["AppSettings:BaseUrl"] ?? "https://localhost:4200";
        await _email.SendAsync("workspace_invite", request.Email, new Dictionary<string, string>
        {
            ["InviterName"] = inviter!.FullName,
            ["WorkspaceName"] = workspace.Name,
            ["AcceptUrl"] = $"{baseUrl}/invitations/accept?token={token}",
            ["DeclineUrl"] = $"{baseUrl}/invitations/decline?token={token}"
        });

        await _audit.LogAsync(inviterId, "INVITATION_SENT", "WorkspaceInvitation", invitation.InvitationId.ToString(), new { workspaceId, to = request.Email });

        return ServiceResult<WorkspaceInvitationResponse>.Success(new WorkspaceInvitationResponse(
            invitation.InvitationId, workspaceId, workspace.Name,
            invitation.InvitedEmail, inviter!.FullName, invitation.Status,
            invitation.ExpiresAt, invitation.CreatedAt), 201);
    }

    public async Task<ServiceResult<IEnumerable<WorkspaceInvitationResponse>>> GetInvitationsAsync(Guid workspaceId, Guid requesterId)
    {
        var isOwner = await _db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == requesterId && m.MemberRole == "Owner" && m.IsActive);
        if (!isOwner) return ServiceResult<IEnumerable<WorkspaceInvitationResponse>>.Forbidden("Not authorized.");

        var workspace = await _db.Workspaces.FindAsync(workspaceId);
        var inviter = await _db.Users.FindAsync(requesterId);

        var invitations = await _db.WorkspaceInvitations
            .Include(i => i.InvitedBy)
            .Where(i => i.WorkspaceId == workspaceId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return ServiceResult<IEnumerable<WorkspaceInvitationResponse>>.Success(invitations.Select(i =>
            new WorkspaceInvitationResponse(i.InvitationId, workspaceId, workspace!.Name,
                i.InvitedEmail, i.InvitedBy.FullName, i.Status, i.ExpiresAt, i.CreatedAt)));
    }

    public async Task<ServiceResult<string>> RevokeInvitationAsync(Guid workspaceId, Guid invitationId, Guid requesterId)
    {
        var isOwner = await _db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == requesterId && m.MemberRole == "Owner" && m.IsActive);
        if (!isOwner) return ServiceResult<string>.Forbidden("Not authorized.");

        var invitation = await _db.WorkspaceInvitations.FirstOrDefaultAsync(i => i.InvitationId == invitationId && i.WorkspaceId == workspaceId);
        if (invitation is null) return ServiceResult<string>.NotFound("Invitation not found.");

        invitation.Status = "Expired";
        await _db.SaveChangesAsync();
        return ServiceResult<string>.Success("Invitation revoked.");
    }

    public async Task<ServiceResult<WorkspaceResponse>> AcceptInvitationAsync(string token, Guid userId)
    {
        var invitation = await _db.WorkspaceInvitations
            .Include(i => i.Workspace).ThenInclude(w => w.Members)
            .FirstOrDefaultAsync(i => i.Token == token && i.Status == "Pending" && i.ExpiresAt > DateTime.UtcNow);

        if (invitation is null)
            return ServiceResult<WorkspaceResponse>.Failure("Invalid or expired invitation token.");

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return ServiceResult<WorkspaceResponse>.NotFound("User not found.");

        if (invitation.InvitedEmail != user.Email.ToLower())
            return ServiceResult<WorkspaceResponse>.Forbidden("This invitation was sent to a different email address.");

        if (invitation.Workspace.Members.Any(m => m.UserId == userId && m.IsActive))
            return ServiceResult<WorkspaceResponse>.Failure("You are already a member of this workspace.");

        _db.WorkspaceMembers.Add(new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = invitation.WorkspaceId,
            UserId = userId,
            MemberRole = MemberRole.Member.ToString(),
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        });

        invitation.Status = InvitationStatus.Accepted.ToString();
        invitation.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "INVITATION_ACCEPTED", "WorkspaceInvitation", invitation.InvitationId.ToString(), new { workspaceId = invitation.WorkspaceId });

        var workspace = await _db.Workspaces.Include(w => w.Owner).Include(w => w.Members).FirstAsync(w => w.WorkspaceId == invitation.WorkspaceId);
        return ServiceResult<WorkspaceResponse>.Success(MapWorkspace(workspace));
    }

    public async Task<ServiceResult<string>> DeclineInvitationAsync(string token, Guid userId)
    {
        var invitation = await _db.WorkspaceInvitations
            .FirstOrDefaultAsync(i => i.Token == token && i.Status == "Pending");

        if (invitation is null) return ServiceResult<string>.NotFound("Invitation not found or already responded.");

        invitation.Status = InvitationStatus.Declined.ToString();
        invitation.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ServiceResult<string>.Success("Invitation declined.");
    }

    private static WorkspaceResponse MapWorkspace(Workspace w) =>
        new(w.WorkspaceId, w.Name, w.Description, w.LogoUrl, w.IsActive,
            w.OwnerId, w.Owner?.FullName ?? string.Empty,
            w.Members?.Count(m => m.IsActive) ?? 0, w.CreatedAt);
}
