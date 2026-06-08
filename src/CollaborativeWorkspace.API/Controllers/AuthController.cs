using CollaborativeWorkspace.Application.DTOs.Auth;
using CollaborativeWorkspace.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace CollaborativeWorkspace.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(IAuthService auth, IValidator<RegisterRequest> registerValidator, IValidator<LoginRequest> loginValidator)
    {
        _auth = auth;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    /// <summary>POST /api/v1/auth/register</summary>
    [HttpPost("register")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var result = await _auth.RegisterAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.StatusCode, result.IsSuccess ? (object)result.Data! : new { error = result.ErrorMessage, code = result.ErrorCode });
    }

    /// <summary>GET /api/v1/auth/verify-email?token=xxx</summary>
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var result = await _auth.VerifyEmailAsync(token);
        if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });
        return Ok(new { message = result.Data });
    }

    /// <summary>POST /api/v1/auth/resend-verification</summary>
    [HttpPost("resend-verification")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        var result = await _auth.ResendVerificationAsync(request);
        return Ok(new { message = result.Data ?? result.ErrorMessage });
    }

    /// <summary>POST /api/v1/auth/login</summary>
    [HttpPost("login")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid) return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = Request.Headers.UserAgent.ToString();
        var result = await _auth.LoginAsync(request, ip, deviceInfo);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.ErrorMessage, code = result.ErrorCode });
        return Ok(result.Data);
    }

    /// <summary>POST /api/v1/auth/refresh</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _auth.RefreshTokenAsync(request);
        if (!result.IsSuccess) return Unauthorized(new { error = result.ErrorMessage });
        return Ok(result.Data);
    }

    /// <summary>POST /api/v1/auth/logout</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _auth.LogoutAsync(request.RefreshToken, userId);
        return Ok(new { message = result.Data });
    }

    /// <summary>GET /api/v1/auth/me</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _auth.GetCurrentUserAsync(userId);
        if (!result.IsSuccess) return NotFound(new { error = result.ErrorMessage });
        return Ok(result.Data);
    }

    /// <summary>POST /api/v1/auth/forgot-password</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _auth.ForgotPasswordAsync(request);
        return Ok(new { message = result.Data });
    }

    /// <summary>POST /api/v1/auth/reset-password</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _auth.ResetPasswordAsync(request);
        if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });
        return Ok(new { message = result.Data });
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
