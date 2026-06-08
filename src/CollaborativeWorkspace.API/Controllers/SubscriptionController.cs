using CollaborativeWorkspace.Application.DTOs.Subscription;
using CollaborativeWorkspace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CollaborativeWorkspace.API.Controllers;

[ApiController]
[Route("api/v1")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptions;

    public SubscriptionController(ISubscriptionService subscriptions) => _subscriptions = subscriptions;

    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        var result = await _subscriptions.GetPlansAsync();
        return Ok(result.Data);
    }

    [HttpGet("subscriptions/me")]
    [Authorize]
    public async Task<IActionResult> GetMySubscription()
    {
        var result = await _subscriptions.GetMySubscriptionAsync(CurrentUserId);
        return Ok(result.Data);
    }

    [HttpPost("subscriptions/checkout")]
    [Authorize]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutRequest request)
    {
        var result = await _subscriptions.CreateCheckoutSessionAsync(CurrentUserId, request);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(result.Data);
    }

    [HttpPost("subscriptions/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelSubscription()
    {
        var result = await _subscriptions.CancelSubscriptionAsync(CurrentUserId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, Problem(result.ErrorMessage));
        return Ok(new { message = result.Data });
    }

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value,
        out var id) ? id : Guid.Empty;
}
