using GermanyApplications.Api.Domain.Enums;

namespace GermanyApplications.Api.Domain.Entities;

public sealed class SavedCourse : EntityBase
{
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public User User { get; set; } = null!;
    public Course Course { get; set; } = null!;
}

public sealed class EligibilityAssessment : EntityBase, IImmutableEntity
{
    public Guid UserId { get; set; }
    public Guid CourseVersionId { get; set; }
    public EligibilityOutcome Outcome { get; set; }
    public DateTimeOffset AssessedAt { get; set; }
    public string InputSnapshotJson { get; set; } = "{}";
    public string RuleSetVersion { get; set; } = string.Empty;
    public DateTimeOffset DisclaimerAcknowledgedAt { get; set; }
    public User User { get; set; } = null!;
    public CourseVersion CourseVersion { get; set; } = null!;
    public ICollection<EligibilityAssessmentItem> Items { get; set; } = [];
}

public sealed class EligibilityAssessmentItem : EntityBase, IImmutableEntity
{
    public Guid EligibilityAssessmentId { get; set; }
    public Guid? CourseRequirementId { get; set; }
    public EligibilityItemResult Result { get; set; }
    public string RequirementSnapshotJson { get; set; } = "{}";
    public string? StudentValueSnapshot { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public EligibilityAssessment EligibilityAssessment { get; set; } = null!;
    public CourseRequirement? CourseRequirement { get; set; }
}

public sealed class StudentApplication : EntityBase
{
    public Guid UserId { get; set; }
    public Guid CourseVersionId { get; set; }
    public Guid? CourseIntakeId { get; set; }
    public Guid? ApplicationRouteId { get; set; }
    public StudentApplicationStatus Status { get; set; }
    public string? ExternalReference { get; set; }
    public DateTimeOffset? SubmittedByStudentAt { get; set; }
    public User User { get; set; } = null!;
    public CourseVersion CourseVersion { get; set; } = null!;
    public CourseIntake? CourseIntake { get; set; }
    public ApplicationRoute? ApplicationRoute { get; set; }
    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = [];
}

public sealed class ApplicationStatusHistory : EntityBase, IImmutableEntity
{
    public Guid UserId { get; set; }
    public Guid StudentApplicationId { get; set; }
    public StudentApplicationStatus FromStatus { get; set; }
    public StudentApplicationStatus ToStatus { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string? Note { get; set; }
    public User User { get; set; } = null!;
    public StudentApplication StudentApplication { get; set; } = null!;
}

public sealed class StudentDocument : EntityBase, ISoftDeletable
{
    public Guid UserId { get; set; }
    public Guid? StudentApplicationId { get; set; }
    public Guid? DocumentRequirementId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? MediaType { get; set; }
    public long? SizeBytes { get; set; }
    public string? Sha256Checksum { get; set; }
    public ChecklistDocumentStatus Status { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public User User { get; set; } = null!;
    public StudentApplication? StudentApplication { get; set; }
    public DocumentRequirement? DocumentRequirement { get; set; }
}

public sealed class Notification : EntityBase
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; }
    public string Locale { get; set; } = "en";
    public string SubjectKey { get; set; } = string.Empty;
    public string BodyKey { get; set; } = string.Empty;
    public string TemplateDataJson { get; set; } = "{}";
    public DateTimeOffset? ScheduledFor { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}

public sealed class ConsentRecord : EntityBase, IImmutableEntity
{
    public Guid UserId { get; set; }
    public ConsentType ConsentType { get; set; }
    public string PolicyVersion { get; set; } = string.Empty;
    public bool Granted { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public string Locale { get; set; } = "en";
    public User User { get; set; } = null!;
}

public sealed class AuditLog : EntityBase, IImmutableEntity
{
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public User? ActorUser { get; set; }
}

public sealed class SupportTicket : EntityBase
{
    public Guid UserId { get; set; }
    public SupportTicketStatus Status { get; set; }
    public SupportTicketPriority Priority { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset? ResolvedAt { get; set; }
    public User User { get; set; } = null!;
}
