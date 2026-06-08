using CollaborativeWorkspace.Application.DTOs.Complaint;
using CollaborativeWorkspace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CollaborativeWorkspace.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class ComplaintController : ControllerBase
{
    private readonly IComplaintService _complaints;

    public ComplaintController(IComplaintService complaints) => _complaints = complaints;

    [HttpPost("complaints")]
    public async Task<IActionResult> SubmitComplaint([FromBody] SubmitComplaintRequest request)
    {
        var result = await _complaints.SubmitComplaintAsync(CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return StatusCode(201, result.Data);
    }

    [HttpGet("complaints/mine")]
    public async Task<IActionResult> GetMyComplaints()
    {
        var result = await _complaints.GetMyComplaintsAsync(CurrentUserId);
        return Ok(result.Data);
    }

    [HttpGet("admin/complaints")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<IActionResult> GetAllComplaints([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        var result = await _complaints.GetAllComplaintsAsync(page, pageSize, status);
        return Ok(result.Data);
    }

    [HttpPut("admin/complaints/{id:guid}")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<IActionResult> ResolveComplaint(Guid id, [FromBody] ResolveComplaintRequest request)
    {
        var result = await _complaints.ResolveComplaintAsync(id, CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value,
        out var id) ? id : Guid.Empty;
}
