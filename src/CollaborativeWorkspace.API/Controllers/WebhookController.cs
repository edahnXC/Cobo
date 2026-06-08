using CollaborativeWorkspace.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CollaborativeWorkspace.API.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly ISubscriptionService _subscriptions;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(ISubscriptionService subscriptions, ILogger<WebhookController> logger)
    {
        _subscriptions = subscriptions;
        _logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        // Read raw body for Stripe signature verification
        using var reader = new StreamReader(HttpContext.Request.Body);
        var json = await reader.ReadToEndAsync();

        if (!Request.Headers.TryGetValue("Stripe-Signature", out var sig))
            return BadRequest(new { error = "Missing Stripe-Signature header." });

        try
        {
            await _subscriptions.HandleStripeWebhookAsync(json, sig!);
            return Ok(new { received = true });
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed.");
            return BadRequest(new { error = "Invalid Stripe signature." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook processing error.");
            return StatusCode(500, new { error = "Webhook processing failed." });
        }
    }
}
