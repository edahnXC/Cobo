using CollaborativeWorkspace.Application.DTOs.Message;
using CollaborativeWorkspace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CollaborativeWorkspace.API.Controllers;

[ApiController]
[Route("api/v1/channels/{cId:guid}")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messages;

    public MessageController(IMessageService messages) => _messages = messages;

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages(Guid cId, [FromQuery] Guid? before, [FromQuery] int pageSize = 50)
    {
        var result = await _messages.GetMessagesAsync(cId, CurrentUserId, before, pageSize);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage(Guid cId, [FromBody] SendMessageRequest request)
    {
        var result = await _messages.SendMessageAsync(cId, CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return StatusCode(201, result.Data);
    }

    [HttpPut("messages/{mId:guid}")]
    public async Task<IActionResult> EditMessage(Guid cId, Guid mId, [FromBody] EditMessageRequest request)
    {
        var result = await _messages.EditMessageAsync(cId, mId, CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    [HttpDelete("messages/{mId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid cId, Guid mId)
    {
        var result = await _messages.DeleteMessageAsync(cId, mId, CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    [HttpPost("messages/{mId:guid}/reactions")]
    public async Task<IActionResult> AddReaction(Guid cId, Guid mId, [FromBody] AddReactionRequest request)
    {
        var result = await _messages.AddReactionAsync(cId, mId, CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    [HttpDelete("messages/{mId:guid}/reactions/{emoji}")]
    public async Task<IActionResult> RemoveReaction(Guid cId, Guid mId, string emoji)
    {
        var result = await _messages.RemoveReactionAsync(cId, mId, CurrentUserId, emoji);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value,
        out var id) ? id : Guid.Empty;
}
