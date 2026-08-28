using GermanyApplications.Api.Domain.Enums;

namespace GermanyApplications.Api.Domain.Entities;

public sealed class University : EntityBase, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string CountryCode { get; set; } = "DE";
    public string? OfficialWebsiteUrl { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public ICollection<Course> Courses { get; set; } = [];
}

public sealed class Course : EntityBase, ISoftDeletable
{
    public Guid UniversityId { get; set; }
    public string StableCode { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public University University { get; set; } = null!;
    public ICollection<CourseVersion> Versions { get; set; } = [];
}

public sealed class CourseVersion : EntityBase
{
    public Guid CourseId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public string DegreeAward { get; set; } = string.Empty;
    public string TeachingLanguage { get; set; } = "English";
    public string? Summary { get; set; }
    public CourseVersionStatus Status { get; set; }
    public bool IsDevelopmentSample { get; set; }
    public Guid? OfficialSourceReferenceId { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Course Course { get; set; } = null!;
    public SourceReference? OfficialSourceReference { get; set; }
    public ICollection<CourseRequirement> Requirements { get; set; } = [];
    public ICollection<CourseIntake> Intakes { get; set; } = [];
    public ICollection<ApplicationRoute> ApplicationRoutes { get; set; } = [];
    public ICollection<Deadline> Deadlines { get; set; } = [];
}

public sealed class SourceReference : EntityBase
{
    public Guid? UniversityId { get; set; }
    public Guid? CourseId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsOfficial { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public University? University { get; set; }
    public Course? Course { get; set; }
}

public sealed class CourseRequirement : EntityBase
{
    public Guid CourseVersionId { get; set; }
    public RequirementType Type { get; set; }
    public RequirementOperator Operator { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SubjectArea { get; set; }
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? Unit { get; set; }
    public string? HumanReadableDescription { get; set; }
    public Guid SourceReferenceId { get; set; }
    public bool IsMandatory { get; set; }
    public int SortOrder { get; set; }
    public CourseVersion CourseVersion { get; set; } = null!;
    public SourceReference SourceReference { get; set; } = null!;
}

public sealed class CourseIntake : EntityBase
{
    public Guid CourseVersionId { get; set; }
    public IntakeTerm Term { get; set; }
    public int? Year { get; set; }
    public string? Label { get; set; }
    public DateOnly? StudyStartDate { get; set; }
    public CourseVersion CourseVersion { get; set; } = null!;
}

public sealed class ApplicationRoute : EntityBase
{
    public Guid CourseVersionId { get; set; }
    public ApplicationRouteType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OfficialApplicationUrl { get; set; } = string.Empty;
    public Guid SourceReferenceId { get; set; }
    public CourseVersion CourseVersion { get; set; } = null!;
    public SourceReference SourceReference { get; set; } = null!;
}

public sealed class Deadline : EntityBase
{
    public Guid CourseVersionId { get; set; }
    public Guid? CourseIntakeId { get; set; }
    public Guid? ApplicationRouteId { get; set; }
    public string DeadlineType { get; set; } = string.Empty;
    public string ApplicantCategory { get; set; } = string.Empty;
    public DateOnly DeadlineDate { get; set; }
    public TimeOnly? DeadlineTime { get; set; }
    public string TimeZoneId { get; set; } = "Europe/Berlin";
    public Guid SourceReferenceId { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
    public CourseVersion CourseVersion { get; set; } = null!;
    public CourseIntake? CourseIntake { get; set; }
    public ApplicationRoute? ApplicationRoute { get; set; }
    public SourceReference SourceReference { get; set; } = null!;
}

public sealed class DocumentRequirement : EntityBase
{
    public Guid CourseVersionId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsMandatory { get; set; }
    public Guid SourceReferenceId { get; set; }
    public int SortOrder { get; set; }
    public CourseVersion CourseVersion { get; set; } = null!;
    public SourceReference SourceReference { get; set; } = null!;
}
