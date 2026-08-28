using GermanyApplications.Api.Domain.Entities;
using GermanyApplications.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GermanyApplications.Api.Data.Configurations;

public static class ModelConfiguration
{
    public static void ApplyApplicationModel(this ModelBuilder modelBuilder)
    {
        ConfigureIdentity(modelBuilder);
        ConfigureStudent(modelBuilder);
        ConfigureCatalogue(modelBuilder);
        ConfigureWorkflow(modelBuilder);

        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.HasQueryFilter(user => !user.IsDeleted);
            entity.Property(user => user.UserName).HasMaxLength(256);
            entity.Property(user => user.NormalizedUserName).HasMaxLength(256);
            entity.Property(user => user.Email).HasMaxLength(256);
            entity.Property(user => user.NormalizedEmail).HasMaxLength(256);
            entity.Property(user => user.CreatedAt).IsRequired();
            entity.Property(user => user.UpdatedAt).IsRequired();
            entity.Property(user => user.ConcurrencyStamp).IsConcurrencyToken();
            entity.HasIndex(user => user.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
            entity.HasIndex(user => user.NormalizedEmail).HasDatabaseName("EmailIndex");
            entity.HasIndex(user => new { user.IsDeleted, user.CreatedAt });
        });
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Name).HasMaxLength(256);
            entity.Property(role => role.NormalizedName).HasMaxLength(256);
            entity.Property(role => role.ConcurrencyStamp).IsConcurrencyToken();
            entity.Property(role => role.CreatedAt).IsRequired();
            entity.Property(role => role.UpdatedAt).IsRequired();
            entity.HasIndex(role => role.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique();
        });
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("user_claims");
            entity.HasKey(claim => claim.Id);
            entity.HasOne<User>().WithMany().HasForeignKey(claim => claim.UserId);
        });
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("user_logins");
            entity.HasKey(login => new { login.LoginProvider, login.ProviderKey });
            entity.Property(login => login.LoginProvider).HasMaxLength(128);
            entity.Property(login => login.ProviderKey).HasMaxLength(128);
            entity.HasOne<User>().WithMany().HasForeignKey(login => login.UserId);
        });
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("user_tokens");
            entity.HasKey(token => new { token.UserId, token.LoginProvider, token.Name });
            entity.Property(token => token.LoginProvider).HasMaxLength(128);
            entity.Property(token => token.Name).HasMaxLength(128);
            entity.HasOne<User>().WithMany().HasForeignKey(token => token.UserId);
        });
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("role_claims");
            entity.HasKey(claim => claim.Id);
            entity.HasOne<Role>().WithMany().HasForeignKey(claim => claim.RoleId);
        });
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
            entity.HasOne<User>().WithMany().HasForeignKey(userRole => userRole.UserId);
            entity.HasOne<Role>().WithMany().HasForeignKey(userRole => userRole.RoleId);
        });
    }

    private static void ConfigureStudent(ModelBuilder modelBuilder)
    {
        ConfigureBase(modelBuilder.Entity<StudentProfile>(), "student_profiles");
        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.HasIndex(profile => profile.UserId).IsUnique();
            entity.HasAlternateKey(profile => new { profile.Id, profile.UserId });
            entity.Property(profile => profile.PreferredLocale).HasMaxLength(10);
            entity.Property(profile => profile.CitizenshipCountryCode).HasMaxLength(2);
            entity.Property(profile => profile.ResidenceCountryCode).HasMaxLength(2);
            entity.HasOne(profile => profile.User).WithOne(user => user.StudentProfile)
                .HasForeignKey<StudentProfile>(profile => profile.UserId);
        });

        ConfigureOwnedStudentEntity(modelBuilder.Entity<AcademicQualification>(), "academic_qualifications");
        modelBuilder.Entity<AcademicQualification>(entity =>
        {
            entity.Property(item => item.QualificationType).HasMaxLength(100).IsRequired();
            entity.Property(item => item.InstitutionName).HasMaxLength(250).IsRequired();
            entity.Property(item => item.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(item => item.SubjectArea).HasMaxLength(200).IsRequired();
            entity.Property(item => item.GradingSystem).HasMaxLength(100);
            entity.Property(item => item.FinalGrade).HasPrecision(10, 4);
            entity.Property(item => item.Credits).HasPrecision(10, 2);
            entity.Property(item => item.CreditSystem).HasMaxLength(50);
            entity.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.StudentProfile).WithMany(profile => profile.AcademicQualifications)
                .HasForeignKey(item => new { item.StudentProfileId, item.UserId })
                .HasPrincipalKey(profile => new { profile.Id, profile.UserId });
        });

        ConfigureOwnedStudentEntity(modelBuilder.Entity<LanguageQualification>(), "language_qualifications");
        modelBuilder.Entity<LanguageQualification>(entity =>
        {
            entity.Property(item => item.LanguageCode).HasMaxLength(10).IsRequired();
            entity.Property(item => item.TestName).HasMaxLength(100).IsRequired();
            entity.Property(item => item.OverallScore).HasPrecision(10, 2);
            entity.Property(item => item.ScoreScale).HasMaxLength(50);
            entity.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.StudentProfile).WithMany(profile => profile.LanguageQualifications)
                .HasForeignKey(item => new { item.StudentProfileId, item.UserId })
                .HasPrincipalKey(profile => new { profile.Id, profile.UserId });
        });

        ConfigureOwnedStudentEntity(modelBuilder.Entity<WorkExperience>(), "work_experiences");
        modelBuilder.Entity<WorkExperience>(entity =>
        {
            entity.Property(item => item.EmployerName).HasMaxLength(250).IsRequired();
            entity.Property(item => item.JobTitle).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Industry).HasMaxLength(150);
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.StudentProfile).WithMany(profile => profile.WorkExperiences)
                .HasForeignKey(item => new { item.StudentProfileId, item.UserId })
                .HasPrincipalKey(profile => new { profile.Id, profile.UserId });
            entity.ToTable("work_experiences", table => table.HasCheckConstraint(
                "ck_work_experience_dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));
        });
    }

    private static void ConfigureCatalogue(ModelBuilder modelBuilder)
    {
        ConfigureBase(modelBuilder.Entity<University>(), "universities");
        modelBuilder.Entity<University>(entity =>
        {
            entity.HasQueryFilter(university => !university.IsDeleted);
            entity.Property(university => university.Name).HasMaxLength(250).IsRequired();
            entity.Property(university => university.City).HasMaxLength(150);
            entity.Property(university => university.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(university => university.OfficialWebsiteUrl).HasMaxLength(2048);
            entity.HasIndex(university => new { university.CountryCode, university.Name });
        });

        ConfigureBase(modelBuilder.Entity<Course>(), "courses");
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasQueryFilter(course => !course.IsDeleted);
            entity.Property(course => course.StableCode).HasMaxLength(100).IsRequired();
            entity.HasIndex(course => new { course.UniversityId, course.StableCode }).IsUnique();
            entity.HasIndex(course => new { course.IsDeleted, course.UniversityId });
        });

        ConfigureBase(modelBuilder.Entity<SourceReference>(), "source_references");
        modelBuilder.Entity<SourceReference>(entity =>
        {
            entity.Property(source => source.Url).HasMaxLength(2048).IsRequired();
            entity.Property(source => source.Title).HasMaxLength(500).IsRequired();
            entity.Property(source => source.VerifiedBy).HasMaxLength(200);
            entity.HasIndex(source => source.Url);
            entity.HasIndex(source => new { source.CourseId, source.IsOfficial, source.VerifiedAt });
        });

        ConfigureBase(modelBuilder.Entity<CourseVersion>(), "course_versions");
        modelBuilder.Entity<CourseVersion>(entity =>
        {
            entity.Property(version => version.Title).HasMaxLength(300).IsRequired();
            entity.Property(version => version.Discipline).HasMaxLength(150).IsRequired();
            entity.Property(version => version.DegreeAward).HasMaxLength(100).IsRequired();
            entity.Property(version => version.TeachingLanguage).HasMaxLength(100).IsRequired();
            entity.Property(version => version.Summary).HasMaxLength(4000);
            entity.Property(version => version.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(version => new { version.CourseId, version.VersionNumber }).IsUnique();
            entity.HasIndex(version => new { version.Status, version.Discipline, version.TeachingLanguage });
            entity.HasIndex(version => version.Title).HasDatabaseName("IX_course_versions_Title_trgm")
                .HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.ToTable("course_versions", table => table.HasCheckConstraint(
                "ck_course_version_publication_source",
                "\"Status\" <> 'Published' OR (\"OfficialSourceReferenceId\" IS NOT NULL AND \"VerifiedAt\" IS NOT NULL AND \"PublishedAt\" IS NOT NULL AND NOT \"IsDevelopmentSample\")"));
        });

        ConfigureBase(modelBuilder.Entity<CourseRequirement>(), "course_requirements");
        modelBuilder.Entity<CourseRequirement>(entity =>
        {
            entity.Property(requirement => requirement.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(requirement => requirement.Operator).HasConversion<string>().HasMaxLength(50);
            entity.Property(requirement => requirement.Name).HasMaxLength(250).IsRequired();
            entity.Property(requirement => requirement.SubjectArea).HasMaxLength(200);
            entity.Property(requirement => requirement.NumericValue).HasPrecision(18, 4);
            entity.Property(requirement => requirement.TextValue).HasMaxLength(1000);
            entity.Property(requirement => requirement.Unit).HasMaxLength(100);
            entity.Property(requirement => requirement.HumanReadableDescription).HasMaxLength(2000);
            entity.HasIndex(requirement => new { requirement.CourseVersionId, requirement.Type, requirement.SortOrder });
            entity.ToTable("course_requirements", table => table.HasCheckConstraint(
                "ck_course_requirement_typed_value",
                "\"NumericValue\" IS NOT NULL OR \"TextValue\" IS NOT NULL OR \"BooleanValue\" IS NOT NULL OR \"Operator\" = 'Informational'"));
        });

        ConfigureBase(modelBuilder.Entity<CourseIntake>(), "course_intakes");
        modelBuilder.Entity<CourseIntake>(entity =>
        {
            entity.Property(intake => intake.Term).HasConversion<string>().HasMaxLength(20);
            entity.Property(intake => intake.Label).HasMaxLength(100);
            entity.HasIndex(intake => new { intake.CourseVersionId, intake.Term, intake.Year });
        });

        ConfigureBase(modelBuilder.Entity<ApplicationRoute>(), "application_routes");
        modelBuilder.Entity<ApplicationRoute>(entity =>
        {
            entity.Property(route => route.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(route => route.Name).HasMaxLength(200).IsRequired();
            entity.Property(route => route.OfficialApplicationUrl).HasMaxLength(2048).IsRequired();
            entity.HasIndex(route => new { route.CourseVersionId, route.Type });
        });

        ConfigureBase(modelBuilder.Entity<Deadline>(), "deadlines");
        modelBuilder.Entity<Deadline>(entity =>
        {
            entity.Property(deadline => deadline.DeadlineType).HasMaxLength(100).IsRequired();
            entity.Property(deadline => deadline.ApplicantCategory).HasMaxLength(150).IsRequired();
            entity.Property(deadline => deadline.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.HasIndex(deadline => new { deadline.DeadlineDate, deadline.ApplicantCategory });
            entity.HasIndex(deadline => new { deadline.CourseVersionId, deadline.CourseIntakeId });
        });

        ConfigureBase(modelBuilder.Entity<DocumentRequirement>(), "document_requirements");
        modelBuilder.Entity<DocumentRequirement>(entity =>
        {
            entity.Property(document => document.DocumentType).HasMaxLength(100).IsRequired();
            entity.Property(document => document.Name).HasMaxLength(250).IsRequired();
            entity.Property(document => document.Description).HasMaxLength(2000);
            entity.HasIndex(document => new { document.CourseVersionId, document.SortOrder });
        });
    }

    private static void ConfigureWorkflow(ModelBuilder modelBuilder)
    {
        ConfigureBase(modelBuilder.Entity<SavedCourse>(), "saved_courses");
        modelBuilder.Entity<SavedCourse>(entity =>
        {
            entity.HasIndex(saved => new { saved.UserId, saved.CourseId }).IsUnique();
            entity.HasIndex(saved => new { saved.UserId, saved.CreatedAt });
        });

        ConfigureBase(modelBuilder.Entity<EligibilityAssessment>(), "eligibility_assessments");
        modelBuilder.Entity<EligibilityAssessment>(entity =>
        {
            entity.Property(assessment => assessment.Outcome).HasConversion<string>().HasMaxLength(50);
            entity.Property(assessment => assessment.InputSnapshotJson).HasColumnType("jsonb");
            entity.Property(assessment => assessment.RuleSetVersion).HasMaxLength(100).IsRequired();
            entity.HasIndex(assessment => new { assessment.UserId, assessment.AssessedAt });
            entity.HasIndex(assessment => new { assessment.CourseVersionId, assessment.Outcome });
        });

        ConfigureBase(modelBuilder.Entity<EligibilityAssessmentItem>(), "eligibility_assessment_items");
        modelBuilder.Entity<EligibilityAssessmentItem>(entity =>
        {
            entity.Property(item => item.Result).HasConversion<string>().HasMaxLength(50);
            entity.Property(item => item.RequirementSnapshotJson).HasColumnType("jsonb");
            entity.Property(item => item.StudentValueSnapshot).HasMaxLength(1000);
            entity.Property(item => item.Explanation).HasMaxLength(2000).IsRequired();
            entity.HasIndex(item => new { item.EligibilityAssessmentId, item.Result });
        });

        ConfigureBase(modelBuilder.Entity<StudentApplication>(), "student_applications");
        modelBuilder.Entity<StudentApplication>(entity =>
        {
            entity.Property(application => application.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(application => application.ExternalReference).HasMaxLength(250);
            entity.HasIndex(application => new { application.UserId, application.Status, application.UpdatedAt });
            entity.HasIndex(application => new { application.CourseVersionId, application.Status });
            entity.HasAlternateKey(application => new { application.Id, application.UserId });
        });

        ConfigureBase(modelBuilder.Entity<ApplicationStatusHistory>(), "application_status_history");
        modelBuilder.Entity<ApplicationStatusHistory>(entity =>
        {
            entity.Property(history => history.FromStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(history => history.ToStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(history => history.Note).HasMaxLength(2000);
            entity.HasIndex(history => new { history.UserId, history.StudentApplicationId, history.ChangedAt });
            entity.HasOne(history => history.StudentApplication).WithMany(application => application.StatusHistory)
                .HasForeignKey(history => new { history.StudentApplicationId, history.UserId })
                .HasPrincipalKey(application => new { application.Id, application.UserId });
        });

        ConfigureBase(modelBuilder.Entity<StudentDocument>(), "student_documents");
        modelBuilder.Entity<StudentDocument>(entity =>
        {
            entity.HasQueryFilter(document => !document.IsDeleted);
            entity.Property(document => document.DocumentType).HasMaxLength(100).IsRequired();
            entity.Property(document => document.DisplayName).HasMaxLength(250).IsRequired();
            entity.Property(document => document.OriginalFileName).HasMaxLength(255);
            entity.Property(document => document.MediaType).HasMaxLength(150);
            entity.Property(document => document.Sha256Checksum).HasMaxLength(64);
            entity.Property(document => document.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(document => new { document.UserId, document.Status, document.UpdatedAt });
            entity.HasIndex(document => document.StudentApplicationId);
            entity.HasOne(document => document.StudentApplication).WithMany()
                .HasForeignKey(document => new { document.StudentApplicationId, document.UserId })
                .HasPrincipalKey(application => new { application.Id, application.UserId });
        });

        ConfigureBase(modelBuilder.Entity<Notification>(), "notifications");
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(notification => notification.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(notification => notification.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(notification => notification.Locale).HasMaxLength(10);
            entity.Property(notification => notification.SubjectKey).HasMaxLength(200).IsRequired();
            entity.Property(notification => notification.BodyKey).HasMaxLength(200).IsRequired();
            entity.Property(notification => notification.TemplateDataJson).HasColumnType("jsonb");
            entity.Property(notification => notification.IdempotencyKey).HasMaxLength(200).IsRequired();
            entity.HasIndex(notification => notification.IdempotencyKey).IsUnique();
            entity.HasIndex(notification => new { notification.UserId, notification.Status, notification.ScheduledFor });
        });

        ConfigureBase(modelBuilder.Entity<ConsentRecord>(), "consent_records");
        modelBuilder.Entity<ConsentRecord>(entity =>
        {
            entity.Property(consent => consent.ConsentType).HasConversion<string>().HasMaxLength(50);
            entity.Property(consent => consent.PolicyVersion).HasMaxLength(100).IsRequired();
            entity.Property(consent => consent.Locale).HasMaxLength(10);
            entity.HasIndex(consent => new { consent.UserId, consent.ConsentType, consent.RecordedAt });
        });

        ConfigureBase(modelBuilder.Entity<AuditLog>(), "audit_logs");
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(audit => audit.Action).HasMaxLength(200).IsRequired();
            entity.Property(audit => audit.TargetType).HasMaxLength(200).IsRequired();
            entity.Property(audit => audit.Outcome).HasMaxLength(50).IsRequired();
            entity.Property(audit => audit.CorrelationId).HasMaxLength(100);
            entity.Property(audit => audit.MetadataJson).HasColumnType("jsonb");
            entity.HasIndex(audit => audit.OccurredAt);
            entity.HasIndex(audit => new { audit.ActorUserId, audit.OccurredAt });
            entity.HasIndex(audit => new { audit.TargetType, audit.TargetId, audit.OccurredAt });
        });

        ConfigureBase(modelBuilder.Entity<SupportTicket>(), "support_tickets");
        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.Property(ticket => ticket.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(ticket => ticket.Priority).HasConversion<string>().HasMaxLength(50);
            entity.Property(ticket => ticket.Subject).HasMaxLength(250).IsRequired();
            entity.Property(ticket => ticket.Description).HasMaxLength(5000).IsRequired();
            entity.HasIndex(ticket => new { ticket.UserId, ticket.Status, ticket.UpdatedAt });
            entity.HasIndex(ticket => new { ticket.Status, ticket.Priority, ticket.CreatedAt });
        });
    }

    private static void ConfigureBase<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName)
        where TEntity : EntityBase
    {
        entity.ToTable(tableName);
        entity.HasKey(item => item.Id);
        entity.Property(item => item.CreatedAt).IsRequired();
        entity.Property(item => item.UpdatedAt).IsRequired();
        entity.Property(item => item.ConcurrencyToken).IsConcurrencyToken().IsRequired();
    }

    private static void ConfigureOwnedStudentEntity<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName)
        where TEntity : EntityBase
    {
        ConfigureBase(entity, tableName);
        entity.HasIndex("UserId");
        entity.HasIndex("StudentProfileId");
    }
}
