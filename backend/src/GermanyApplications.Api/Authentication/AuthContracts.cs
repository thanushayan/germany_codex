namespace GermanyApplications.Api.Authentication;

public sealed record RegisterRequest(string Email, string Password, string PreferredLocale);
public sealed record LoginRequest(string Email, string Password);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(Guid UserId, string Code, string NewPassword);
public sealed record VerifyEmailRequest(Guid UserId, string Code);
public sealed record CurrentUserResponse(Guid Id, string Email, string PreferredLocale, IReadOnlyCollection<string> Roles);
public sealed record CsrfTokenResponse(string Token);
public sealed record MessageResponse(string Message);
