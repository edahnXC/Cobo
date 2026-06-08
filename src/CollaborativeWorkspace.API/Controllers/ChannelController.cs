using CollaborativeWorkspace.Application.DTOs.Channel;
using CollaborativeWorkspace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CollaborativeWorkspace.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{wId:guid}/channels")]
[Authorize]
public class ChannelController : ControllerBase
{
    private readonly IChannelService _channels;

    public ChannelController(IChannelService channels) => _channels = channels;

    [HttpGet]
    public async Task<IActionResult> GetChannels(Guid wId)
    {
        var result = await _channels.GetChannelsAsync(wId, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateChannel(Guid wId, [FromBody] CreateChannelRequest request)
    {
        var result = await _channels.CreateChannelAsync(wId, CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return StatusCode(201, result.Data);
    }

    [HttpGet("{cId:guid}")]
    public async Task<IActionResult> GetChannel(Guid wId, Guid cId)
    {
        var result = await _channels.GetChannelAsync(wId, cId, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    [HttpPut("{cId:guid}")]
    public async Task<IActionResult> UpdateChannel(Guid wId, Guid cId, [FromBody] UpdateChannelRequest request)
    {
        var result = await _channels.UpdateChannelAsync(wId, cId, CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    [HttpDelete("{cId:guid}")]
    public async Task<IActionResult> DeleteChannel(Guid wId, Guid cId)
    {
        var result = await _channels.DeleteChannelAsync(wId, cId, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    [HttpPost("{cId:guid}/members")]
    public async Task<IActionResult> AddChannelMember(Guid wId, Guid cId, [FromBody] AddChannelMemberRequest request)
    {
        var result = await _channels.AddChannelMemberAsync(wId, cId, CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    [HttpDelete("{cId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveChannelMember(Guid wId, Guid cId, Guid userId)
    {
        var result = await _channels.RemoveChannelMemberAsync(wId, cId, userId, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value,
        out var id) ? id : Guid.Empty;
}
