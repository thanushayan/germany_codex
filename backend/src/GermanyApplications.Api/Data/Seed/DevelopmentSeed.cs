using GermanyApplications.Api.Domain.Entities;
using GermanyApplications.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GermanyApplications.Api.Data.Seed;

public static class DevelopmentSeed
{
    public static readonly Guid StudentRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid ContentAuthorRoleId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid ContentReviewerRoleId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid PrivacySupportRoleId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid SecurityAdministratorRoleId = Guid.Parse("10000000-0000-0000-0000-000000000005");

    private static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            CreateRole(StudentRoleId, "Student"),
            CreateRole(ContentAuthorRoleId, "ContentAuthor"),
            CreateRole(ContentReviewerRoleId, "ContentReviewer"),
            CreateRole(PrivacySupportRoleId, "PrivacySupport"),
            CreateRole(SecurityAdministratorRoleId, "SecurityAdministrator"));

        var universityId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        modelBuilder.Entity<University>().HasData(new
        {
            Id = universityId,
            Name = "[SYNTHETIC DEVELOPMENT DATA] Example German University",
            City = "Example City",
            CountryCode = "DE",
            OfficialWebsiteUrl = (string?)null,
            IsDeleted = false,
            DeletedAt = (DateTimeOffset?)null,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt,
            ConcurrencyToken = Guid.Parse("21000000-0000-0000-0000-000000000001")
        });

        SeedDraftCourse(
            modelBuilder,
            universityId,
            Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Guid.Parse("31000000-0000-0000-0000-000000000001"),
            "DEV-SYNTHETIC-CS",
            "[SYNTHETIC DEVELOPMENT DATA] Example Computer Science MSc",
            "Computer Science");
        SeedDraftCourse(
            modelBuilder,
            universityId,
            Guid.Parse("30000000-0000-0000-0000-000000000002"),
            Guid.Parse("31000000-0000-0000-0000-000000000002"),
            "DEV-SYNTHETIC-DS",
            "[SYNTHETIC DEVELOPMENT DATA] Example Data Science MSc",
            "Data Science");
    }

    private static Role CreateRole(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id.ToString("N"),
        CreatedAt = SeededAt,
        UpdatedAt = SeededAt
    };

    private static void SeedDraftCourse(
        ModelBuilder modelBuilder,
        Guid universityId,
        Guid courseId,
        Guid versionId,
        string stableCode,
        string title,
        string discipline)
    {
        modelBuilder.Entity<Course>().HasData(new
        {
            Id = courseId,
            UniversityId = universityId,
            StableCode = stableCode,
            IsDeleted = false,
            DeletedAt = (DateTimeOffset?)null,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt,
            ConcurrencyToken = courseId
        });
        modelBuilder.Entity<CourseVersion>().HasData(new
        {
            Id = versionId,
            CourseId = courseId,
            VersionNumber = 1,
            Title = title,
            Discipline = discipline,
            DegreeAward = "Master of Science",
            TeachingLanguage = "English",
            Summary = "Synthetic draft used only to verify development catalogue plumbing. It is not a real programme.",
            Status = CourseVersionStatus.Draft,
            IsDevelopmentSample = true,
            OfficialSourceReferenceId = (Guid?)null,
            VerifiedAt = (DateTimeOffset?)null,
            PublishedAt = (DateTimeOffset?)null,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt,
            ConcurrencyToken = versionId
        });
    }
}
