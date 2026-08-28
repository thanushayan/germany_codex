using System.Security.Claims;
using System.Text;
using FluentValidation;
using GermanyApplications.Api.Authorization;
using GermanyApplications.Api.Data;
using GermanyApplications.Api.Domain.Entities;
using GermanyApplications.Api.Email;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;

namespace GermanyApplications.Api.Authentication;

public static class AuthEndpoints
{
    private const string GenericAuthenticationError = "Unable to sign in with the supplied credentials.";
    private const string GenericRecoveryMessage = "If the account exists, password reset instructions will be sent.";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");

        group.MapGet("/csrf", (IAntiforgery antiforgery, HttpContext context) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new CsrfTokenResponse(tokens.RequestToken!));
        }).AllowAnonymous();

        group.MapPost("/register", RegisterAsync)
            .RequireAntiforgery()
            .RequireRateLimiting("registration")
            .AllowAnonymous();
        group.MapPost("/login", LoginAsync)
            .RequireAntiforgery()
            .RequireRateLimiting("authentication")
            .AllowAnonymous();
        group.MapPost("/logout", LogoutAsync)
            .RequireAntiforgery()
            .RequireAuthorization();
        group.MapGet("/me", CurrentUserAsync).RequireAuthorization();
        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .RequireAntiforgery()
            .RequireRateLimiting("password-recovery")
            .AllowAnonymous();
        group.MapPost("/reset-password", ResetPasswordAsync)
            .RequireAntiforgery()
            .RequireRateLimiting("password-recovery")
            .AllowAnonymous();
        group.MapPost("/verify-email", VerifyEmailAsync)
            .RequireAntiforgery()
            .RequireRateLimiting("password-recovery")
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        UserManager<User> userManager,
        ApplicationDbContext dbContext,
        IAccountEmailSender emailSender,
        ISecurityAuditWriter auditWriter,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var email = request.Email.Trim();
        var user = new User { Id = Guid.NewGuid(), UserName = email, Email = email };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            await auditWriter.WriteAsync(null, "account.registration", "rejected", context, cancellationToken);
            return Results.BadRequest(new MessageResponse("Unable to create the account."));
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Student);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Results.Problem("Unable to create the account.", statusCode: StatusCodes.Status500InternalServerError);
        }

        dbContext.StudentProfiles.Add(new StudentProfile
        {
            UserId = user.Id,
            PreferredLocale = request.PreferredLocale
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailSender.SendEmailVerificationAsync(user.Id, email, EncodeToken(code), cancellationToken);
        await auditWriter.WriteAsync(user.Id, "account.registration", "succeeded", context, cancellationToken);

        return Results.Accepted(value: new MessageResponse("Account created. Check your email to verify the address."));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ISecurityAuditWriter auditWriter,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        var result = user is null
            ? SignInResult.Failed
            : await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            await auditWriter.WriteAsync(user?.Id, "account.login", result.IsLockedOut ? "locked-out" : "failed", context, cancellationToken);
            return Results.Json(new MessageResponse(GenericAuthenticationError), statusCode: StatusCodes.Status401Unauthorized);
        }

        await auditWriter.WriteAsync(user!.Id, "account.login", "succeeded", context, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAsync(
        SignInManager<User> signInManager,
        ISecurityAuditWriter auditWriter,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context.User);
        await signInManager.SignOutAsync();
        await auditWriter.WriteAsync(userId, "account.logout", "succeeded", context, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> CurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<User> userManager,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var locale = await dbContext.StudentProfiles
            .Where(profile => profile.UserId == user.Id)
            .Select(profile => profile.PreferredLocale)
            .SingleOrDefaultAsync(cancellationToken) ?? "en";

        return Results.Ok(new CurrentUserResponse(user.Id, user.Email!, locale, roles));
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        IValidator<ForgotPasswordRequest> validator,
        UserManager<User> userManager,
        IAccountEmailSender emailSender,
        ISecurityAuditWriter auditWriter,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.IsValid)
        {
            var user = await userManager.FindByEmailAsync(request.Email.Trim());
            if (user is not null && await userManager.IsEmailConfirmedAsync(user))
            {
                var code = await userManager.GeneratePasswordResetTokenAsync(user);
                await emailSender.SendPasswordResetAsync(user.Id, user.Email!, EncodeToken(code), cancellationToken);
                await auditWriter.WriteAsync(user.Id, "account.password-reset-requested", "accepted", context, cancellationToken);
            }
        }

        return Results.Accepted(value: new MessageResponse(GenericRecoveryMessage));
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        IValidator<ResetPasswordRequest> validator,
        UserManager<User> userManager,
        ISecurityAuditWriter auditWriter,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        var code = DecodeToken(request.Code);
        if (user is null || code is null)
        {
            return Results.BadRequest(new MessageResponse("The password reset request is invalid or expired."));
        }

        var result = await userManager.ResetPasswordAsync(user, code, request.NewPassword);
        await auditWriter.WriteAsync(user.Id, "account.password-reset", result.Succeeded ? "succeeded" : "failed", context, cancellationToken);
        if (!result.Succeeded)
        {
            return Results.BadRequest(new MessageResponse("The password reset request is invalid or expired."));
        }

        await userManager.UpdateSecurityStampAsync(user);
        return Results.NoContent();
    }

    private static async Task<IResult> VerifyEmailAsync(
        VerifyEmailRequest request,
        IValidator<VerifyEmailRequest> validator,
        UserManager<User> userManager,
        ISecurityAuditWriter auditWriter,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        var code = DecodeToken(request.Code);
        if (user is null || code is null)
        {
            return Results.BadRequest(new MessageResponse("The email verification request is invalid or expired."));
        }

        var result = await userManager.ConfirmEmailAsync(user, code);
        await auditWriter.WriteAsync(user.Id, "account.email-verification", result.Succeeded ? "succeeded" : "failed", context, cancellationToken);
        return result.Succeeded
            ? Results.NoContent()
            : Results.BadRequest(new MessageResponse("The email verification request is invalid or expired."));
    }

    private static string EncodeToken(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string? DecodeToken(string encodedToken)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
}
