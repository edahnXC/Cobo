using CollaborativeWorkspace.Application.Common;
using CollaborativeWorkspace.Application.DTOs.Auth;
using CollaborativeWorkspace.Application.Interfaces;
using CollaborativeWorkspace.Domain.Entities;
using CollaborativeWorkspace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BC = BCrypt.Net.BCrypt;

namespace CollaborativeWorkspace.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IJwtService jwt, IEmailService email, IAuditService audit, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _email = email;
        _audit = audit;
        _config = config;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, string? ipAddress = null)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            return ServiceResult<AuthResponse>.Failure("Email address is already registered.", "EMAIL_TAKEN");

        var verifyToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var user = new User
        {
            UserId = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BC.HashPassword(request.Password, workFactor: 12),
            IsEmailVerified = false,
            EmailVerifyToken = verifyToken,
            EmailVerifyExpiry = DateTime.UtcNow.AddHours(24),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        // Assign default Member role (RoleId = 4)
        _db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = 4 });

        await _db.SaveChangesAsync();

        var baseUrl = _config["AppSettings:BaseUrl"] ?? "https://localhost:4200";
        var verifyUrl = $"{baseUrl}/auth/verify-email?token={verifyToken}";

        await _email.SendAsync("email_verification", user.Email, new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["VerifyUrl"] = verifyUrl
        }, user.UserId);

        await _audit.LogAsync(user.UserId, "USER_REGISTER", "User", user.UserId.ToString(),
            new { user.Email }, ipAddress);

        // Registration does NOT return a token — user must verify email first
        return ServiceResult<AuthResponse>.Failure(
            "Registration successful. Please check your email to verify your account before logging in.",
            "VERIFY_EMAIL_REQUIRED",
            202);
    }

    public async Task<ServiceResult<string>> VerifyEmailAsync(string token)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.EmailVerifyToken == token &&
            u.EmailVerifyExpiry > DateTime.UtcNow);

        if (user is null)
            return ServiceResult<string>.Failure("Invalid or expired verification token.", "INVALID_TOKEN");

        user.IsEmailVerified = true;
        user.EmailVerifyToken = null;
        user.EmailVerifyExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(user.UserId, "EMAIL_VERIFIED", "User", user.UserId.ToString());
        return ServiceResult<string>.Success("Email verified successfully. You may now log in.");
    }

    public async Task<ServiceResult<string>> ResendVerificationAsync(ResendVerificationRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());
        if (user is null)
            return ServiceResult<string>.Success("If that email exists, a new verification link has been sent.");
        if (user.IsEmailVerified)
            return ServiceResult<string>.Failure("Email is already verified.", "ALREADY_VERIFIED");

        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        user.EmailVerifyToken = token;
        user.EmailVerifyExpiry = DateTime.UtcNow.AddHours(24);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var baseUrl = _config["AppSettings:BaseUrl"] ?? "https://localhost:4200";
        var verifyUrl = $"{baseUrl}/auth/verify-email?token={token}";
        await _email.SendAsync("email_verification_resend", user.Email, new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["VerifyUrl"] = verifyUrl
        }, user.UserId);

        return ServiceResult<string>.Success("Verification email resent.");
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null, string? deviceInfo = null)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user is null || !BC.Verify(request.Password, user.PasswordHash))
            return ServiceResult<AuthResponse>.Failure("Invalid email or password.", "INVALID_CREDENTIALS");

        if (!user.IsEmailVerified)
            return ServiceResult<AuthResponse>.Failure("Please verify your email before logging in.", "EMAIL_NOT_VERIFIED");

        if (!user.IsActive)
            return ServiceResult<AuthResponse>.Failure("Your account has been deactivated. Contact support.", "ACCOUNT_INACTIVE");

        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        var accessToken = _jwt.GenerateAccessToken(user, roles);
        var refreshTokenValue = _jwt.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            TokenId = Guid.NewGuid(),
            UserId = user.UserId,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.RefreshTokens.Add(refreshToken);

        var session = new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = user.UserId,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            LoginAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            IsActive = true
        };
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        // Get active plan
        var activeSub = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == user.UserId && s.Status == "Active" && s.EndDate > DateTime.UtcNow)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        await _audit.LogAsync(user.UserId, "USER_LOGIN", "User", user.UserId.ToString(), new { ipAddress }, ipAddress);

        var profile = MapUserToProfile(user, roles, activeSub?.Plan.PlanName ?? "Free");
        return ServiceResult<AuthResponse>.Success(new AuthResponse(
            accessToken,
            refreshTokenValue,
            DateTime.UtcNow.AddMinutes(60),
            profile
        ));
    }

    public async Task<ServiceResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var token = await _db.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);

        if (token is null)
            return ServiceResult<AuthResponse>.Failure("Invalid or expired refresh token.", "INVALID_REFRESH_TOKEN");

        var user = token.User;

        // Rotate: revoke old, issue new
        token.IsRevoked = true;

        var newRefreshValue = _jwt.GenerateRefreshToken();
        var newRefresh = new RefreshToken
        {
            TokenId = Guid.NewGuid(),
            UserId = user.UserId,
            Token = newRefreshValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.RefreshTokens.Add(newRefresh);
        await _db.SaveChangesAsync();

        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        var newAccess = _jwt.GenerateAccessToken(user, roles);

        var activeSub = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == user.UserId && s.Status == "Active" && s.EndDate > DateTime.UtcNow)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        var profile = MapUserToProfile(user, roles, activeSub?.Plan.PlanName ?? "Free");
        return ServiceResult<AuthResponse>.Success(new AuthResponse(newAccess, newRefreshValue, DateTime.UtcNow.AddMinutes(60), profile));
    }

    public async Task<ServiceResult<string>> LogoutAsync(string refreshToken, Guid userId)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

        if (token is not null)
        {
            token.IsRevoked = true;
            await _db.SaveChangesAsync();
        }

        // Mark active sessions as ended
        var activeSessions = await _db.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        foreach (var s in activeSessions)
        {
            s.IsActive = false;
            s.LogoutAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "USER_LOGOUT");
        return ServiceResult<string>.Success("Logged out successfully.");
    }

    public async Task<ServiceResult<UserProfileResponse>> GetCurrentUserAsync(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user is null) return ServiceResult<UserProfileResponse>.NotFound("User not found.");

        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        var activeSub = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.Status == "Active" && s.EndDate > DateTime.UtcNow)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        return ServiceResult<UserProfileResponse>.Success(MapUserToProfile(user, roles, activeSub?.Plan.PlanName ?? "Free"));
    }

    public async Task<ServiceResult<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());
        // Always return success to prevent user enumeration
        if (user is null)
            return ServiceResult<string>.Success("If that email is registered, a password reset link has been sent.");

        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        user.PasswordResetToken = token;
        user.PasswordResetExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var baseUrl = _config["AppSettings:BaseUrl"] ?? "https://localhost:4200";
        var resetUrl = $"{baseUrl}/auth/reset-password?token={token}";
        await _email.SendAsync("password_reset", user.Email, new Dictionary<string, string>
        {
            ["FullName"] = user.FullName,
            ["ResetUrl"] = resetUrl
        }, user.UserId);

        return ServiceResult<string>.Success("If that email is registered, a password reset link has been sent.");
    }

    public async Task<ServiceResult<string>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == request.Token &&
            u.PasswordResetExpiry > DateTime.UtcNow);

        if (user is null)
            return ServiceResult<string>.Failure("Invalid or expired password reset token.", "INVALID_TOKEN");

        user.PasswordHash = BC.HashPassword(request.NewPassword, workFactor: 12);
        user.PasswordResetToken = null;
        user.PasswordResetExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Revoke all refresh tokens on password change
        var tokens = await _db.RefreshTokens.Where(rt => rt.UserId == user.UserId && !rt.IsRevoked).ToListAsync();
        tokens.ForEach(t => t.IsRevoked = true);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(user.UserId, "PASSWORD_RESET", "User", user.UserId.ToString());
        return ServiceResult<string>.Success("Password has been reset successfully. Please log in.");
    }

    private static UserProfileResponse MapUserToProfile(User user, IEnumerable<string> roles, string? plan)
        => new(user.UserId, user.FullName, user.Email, user.IsEmailVerified, user.AvatarUrl, roles, plan, user.CreatedAt);
}
