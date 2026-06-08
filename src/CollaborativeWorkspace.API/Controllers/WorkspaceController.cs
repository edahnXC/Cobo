using CollaborativeWorkspace.Application.DTOs.Workspace;
using CollaborativeWorkspace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CollaborativeWorkspace.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class WorkspaceController : ControllerBase
{
    private readonly IWorkspaceService _workspaces;

    public WorkspaceController(IWorkspaceService workspaces) => _workspaces = workspaces;

    [HttpGet("workspaces")]
    public async Task<IActionResult> GetMyWorkspaces()
    {
        var result = await _workspaces.GetMyWorkspacesAsync(CurrentUserId);
        return Ok(result.Data);
    }

    [HttpPost("workspaces")]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request)
    {
        var result = await _workspaces.CreateWorkspaceAsync(CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return StatusCode(201, result.Data);
    }

    [HttpGet("workspaces/{id:guid}")]
    public async Task<IActionResult> GetWorkspace(Guid id)
    {
        var result = await _workspaces.GetWorkspaceAsync(id, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    [HttpPut("workspaces/{id:guid}")]
    public async Task<IActionResult> UpdateWorkspace(Guid id, [FromBody] UpdateWorkspaceRequest request)
    {
        var result = await _workspaces.UpdateWorkspaceAsync(id, CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    [HttpDelete("workspaces/{id:guid}")]
    public async Task<IActionResult> DeleteWorkspace(Guid id)
    {
        var result = await _workspaces.DeleteWorkspaceAsync(id, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    [HttpGet("workspaces/{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var result = await _workspaces.GetMembersAsync(id, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    [HttpDelete("workspaces/{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        var result = await _workspaces.RemoveMemberAsync(id, userId, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    [HttpPut("workspaces/{id:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid id, Guid userId, [FromBody] UpdateMemberRoleRequest request)
    {
        var result = await _workspaces.UpdateMemberRoleAsync(id, userId, CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    [HttpPost("workspaces/{id:guid}/invitations")]
    public async Task<IActionResult> SendInvitation(Guid id, [FromBody] InviteMemberRequest request)
    {
        var result = await _workspaces.SendInvitationAsync(id, CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return StatusCode(201, result.Data);
    }

    [HttpGet("workspaces/{id:guid}/invitations")]
    public async Task<IActionResult> GetInvitations(Guid id)
    {
        var result = await _workspaces.GetInvitationsAsync(id, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    [HttpDelete("workspaces/{id:guid}/invitations/{invId:guid}")]
    public async Task<IActionResult> RevokeInvitation(Guid id, Guid invId)
    {
        var result = await _workspaces.RevokeInvitationAsync(id, invId, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    [HttpPost("invitations/accept")]
    public async Task<IActionResult> AcceptInvitation([FromQuery] string token)
    {
        var result = await _workspaces.AcceptInvitationAsync(token, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    [HttpPost("invitations/decline")]
    public async Task<IActionResult> DeclineInvitation([FromQuery] string token)
    {
        var result = await _workspaces.DeclineInvitationAsync(token, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value,
        out var id) ? id : Guid.Empty;
}
