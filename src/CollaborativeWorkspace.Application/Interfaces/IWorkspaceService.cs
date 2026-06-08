using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Workspace;

namespace CollaborativeWorkspace.Application.Interfaces;

public interface IWorkspaceService
{
    Task<ServiceResult<IEnumerable<WorkspaceResponse>>> GetMyWorkspacesAsync(Guid userId);
    Task<ServiceResult<WorkspaceResponse>> CreateWorkspaceAsync(Guid userId, CreateWorkspaceRequest request);
    Task<ServiceResult<WorkspaceResponse>> GetWorkspaceAsync(Guid workspaceId, Guid userId);
    Task<ServiceResult<WorkspaceResponse>> UpdateWorkspaceAsync(Guid workspaceId, Guid userId, UpdateWorkspaceRequest request);
    Task<ServiceResult<string>> DeleteWorkspaceAsync(Guid workspaceId, Guid userId);

    Task<ServiceResult<IEnumerable<WorkspaceMemberResponse>>> GetMembersAsync(Guid workspaceId, Guid requesterId);
    Task<ServiceResult<string>> RemoveMemberAsync(Guid workspaceId, Guid targetUserId, Guid requesterId);
    Task<ServiceResult<string>> UpdateMemberRoleAsync(Guid workspaceId, Guid targetUserId, Guid requesterId, UpdateMemberRoleRequest request);

    Task<ServiceResult<WorkspaceInvitationResponse>> SendInvitationAsync(Guid workspaceId, Guid inviterId, InviteMemberRequest request);
    Task<ServiceResult<IEnumerable<WorkspaceInvitationResponse>>> GetInvitationsAsync(Guid workspaceId, Guid requesterId);
    Task<ServiceResult<string>> RevokeInvitationAsync(Guid workspaceId, Guid invitationId, Guid requesterId);
    Task<ServiceResult<WorkspaceResponse>> AcceptInvitationAsync(string token, Guid userId);
    Task<ServiceResult<string>> DeclineInvitationAsync(string token, Guid userId);
}
