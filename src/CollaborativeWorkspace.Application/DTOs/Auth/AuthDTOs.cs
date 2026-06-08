namespace CollaborativeWorkspace.Application.DTOs.Auth;

// ─── Requests ─────────────────────────────────────────────────────────────────

public record RegisterRequest(
    string FullName,
    string Email,
    string Password
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record ForgotPasswordRequest(
    string Email
);

public record ResetPasswordRequest(
    string Token,
    string NewPassword
);

public record ResendVerificationRequest(
    string Email
);

// ─── Responses ────────────────────────────────────────────────────────────────

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserProfileResponse User
);

public record UserProfileResponse(
    Guid UserId,
    string FullName,
    string Email,
    bool IsEmailVerified,
    string? AvatarUrl,
    IEnumerable<string> Roles,
    string? ActivePlan,
    DateTime CreatedAt
);
