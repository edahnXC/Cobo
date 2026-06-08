using CollaborativeWorkspace.Application.DTOs.Admin;
using CollaborativeWorkspace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollaborativeWorkspace.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "PlatformAdmin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin) => _admin = admin;

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var result = await _admin.GetUsersAsync(page, pageSize, search);
        return Ok(result.Data);
    }

    [HttpPut("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var result = await _admin.UpdateUserStatusAsync(id, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? action = null,
        [FromQuery] Guid? userId = null)
    {
        var result = await _admin.GetAuditLogsAsync(page, pageSize, action, userId);
        return Ok(result.Data);
    }

    [HttpGet("email-logs")]
    public async Task<IActionResult> GetEmailLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        var result = await _admin.GetEmailLogsAsync(page, pageSize, status);
        return Ok(result.Data);
    }
}
