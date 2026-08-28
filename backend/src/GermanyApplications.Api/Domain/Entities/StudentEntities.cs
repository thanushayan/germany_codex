namespace GermanyApplications.Api.Domain.Entities;

public sealed class StudentProfile : EntityBase
{
    public Guid UserId { get; set; }
    public string? PreferredLocale { get; set; }
    public string? CitizenshipCountryCode { get; set; }
    public string? ResidenceCountryCode { get; set; }
    public DateOnly? ExpectedStudyStartDate { get; set; }
    public User User { get; set; } = null!;
    public ICollection<AcademicQualification> AcademicQualifications { get; set; } = [];
    public ICollection<LanguageQualification> LanguageQualifications { get; set; } = [];
    public ICollection<WorkExperience> WorkExperiences { get; set; } = [];
}

public sealed class AcademicQualification : EntityBase
{
    public Guid UserId { get; set; }
    public Guid StudentProfileId { get; set; }
    public string QualificationType { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string SubjectArea { get; set; } = string.Empty;
    public string? GradingSystem { get; set; }
    public decimal? FinalGrade { get; set; }
    public decimal? Credits { get; set; }
    public string? CreditSystem { get; set; }
    public DateOnly? GraduationDate { get; set; }
    public bool IsCompleted { get; set; }
    public User User { get; set; } = null!;
    public StudentProfile StudentProfile { get; set; } = null!;
}

public sealed class LanguageQualification : EntityBase
{
    public Guid UserId { get; set; }
    public Guid StudentProfileId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public decimal? OverallScore { get; set; }
    public string? ScoreScale { get; set; }
    public DateOnly? TestDate { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public User User { get; set; } = null!;
    public StudentProfile StudentProfile { get; set; } = null!;
}

public sealed class WorkExperience : EntityBase
{
    public Guid UserId { get; set; }
    public Guid StudentProfileId { get; set; }
    public string EmployerName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }
    public User User { get; set; } = null!;
    public StudentProfile StudentProfile { get; set; } = null!;
}
