using GermanyApplications.Api.Data;
using GermanyApplications.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_tests;Username=test;Password=test")
            .Options;
        return new ApplicationDbContext(options);
    }
}
