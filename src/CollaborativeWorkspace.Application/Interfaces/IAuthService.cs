using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Auth;

namespace CollaborativeWorkspace.Application.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, string? ipAddress = null);
    Task<ServiceResult<string>> VerifyEmailAsync(string token);
    Task<ServiceResult<string>> ResendVerificationAsync(ResendVerificationRequest request);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null, string? deviceInfo = null);
    Task<ServiceResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<ServiceResult<string>> LogoutAsync(string refreshToken, Guid userId);
    Task<ServiceResult<DTOs.Auth.UserProfileResponse>> GetCurrentUserAsync(Guid userId);
    Task<ServiceResult<string>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ServiceResult<string>> ResetPasswordAsync(ResetPasswordRequest request);
}
