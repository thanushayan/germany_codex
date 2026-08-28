using GermanyApplications.Api.Data;
using GermanyApplications.Api.Authorization;
using GermanyApplications.Api.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GermanyApplications.Api.Tests;

public sealed class DatabaseModelTests
{
    [Fact]
    public void Model_UsesRestrictForEveryForeignKey()
    {
        using var context = CreateContext();

        var unsafeForeignKeys = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Where(foreignKey => foreignKey.DeleteBehavior != DeleteBehavior.Restrict)
            .Select(foreignKey => $"{foreignKey.DeclaringEntityType.Name}->{foreignKey.PrincipalEntityType.Name}")
            .ToArray();

        Assert.Empty(unsafeForeignKeys);
    }

    [Fact]
    public void Model_PublishedCourseVersionHasDatabaseSourceConstraint()
    {
        using var context = CreateContext();

        var constraints = context.Model.FindEntityType(typeof(CourseVersion))!
            .GetCheckConstraints()
            .Select(constraint => constraint.Name);

        Assert.Contains("ck_course_version_publication_source", constraints);
    }

    [Fact]
    public void SaveChanges_RejectsModificationOfHistoricalAssessment()
    {
        using var context = CreateContext();
        var assessment = new EligibilityAssessment { Id = Guid.NewGuid() };
        context.Attach(assessment);
        assessment.RuleSetVersion = "changed";
        context.Entry(assessment).State = EntityState.Modified;

        var exception = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());

        Assert.Contains("immutable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StudentOwnershipPolicy_AllowsOwnerAndRejectsAnotherStudent()
    {
        var ownerId = Guid.NewGuid();
        var requirement = new StudentOwnershipRequirement();
        var handler = new StudentOwnershipHandler();
        var owner = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, ownerId.ToString()), new Claim(ClaimTypes.Role, AppRoles.Student)],
            "test"));
        var otherStudent = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, AppRoles.Student)],
            "test"));

        var ownerContext = new AuthorizationHandlerContext([requirement], owner, new StudentOwnedResource(ownerId));
        await handler.HandleAsync(ownerContext);
        var otherContext = new AuthorizationHandlerContext([requirement], otherStudent, new StudentOwnedResource(ownerId));
        await handler.HandleAsync(otherContext);

        Assert.True(ownerContext.HasSucceeded);
        Assert.False(otherContext.HasSucceeded);
    }

    [Fact]
    public async Task StudentOwnershipPolicy_AllowsExplicitAdministratorOverride()
    {
        var requirement = new StudentOwnershipRequirement();
        var administrator = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, AppRoles.Admin)],
            "test"));
        var context = new AuthorizationHandlerContext(
            [requirement],
            administrator,
            new StudentOwnedResource(Guid.NewGuid()));

        await new StudentOwnershipHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_tests;Username=test;Password=test")
            .Options;
        return new ApplicationDbContext(options);
    }
}
