using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace GermanyApplications.Api.Authorization;

public sealed record StudentOwnedResource(Guid UserId);

public sealed class StudentOwnershipRequirement : IAuthorizationRequirement;

public sealed class StudentOwnershipHandler : AuthorizationHandler<StudentOwnershipRequirement, StudentOwnedResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        StudentOwnershipRequirement requirement,
        StudentOwnedResource resource)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ownsResource = Guid.TryParse(userIdValue, out var userId) && userId == resource.UserId;
        var privileged = context.User.IsInRole(AppRoles.Admin) || context.User.IsInRole(AppRoles.SuperAdmin);

        if (ownsResource || privileged)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public static class AppRoles
{
    public const string Student = "Student";
    public const string ContentEditor = "ContentEditor";
    public const string Reviewer = "Reviewer";
    public const string SupportAgent = "SupportAgent";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";

    public static readonly string[] All = [Student, ContentEditor, Reviewer, SupportAgent, Admin, SuperAdmin];
}

public static class AuthorizationPolicies
{
    public const string ContentManagement = "ContentManagement";
    public const string Review = "Review";
    public const string Support = "Support";
    public const string Administration = "Administration";
    public const string OwnsStudentResource = "OwnsStudentResource";
}
