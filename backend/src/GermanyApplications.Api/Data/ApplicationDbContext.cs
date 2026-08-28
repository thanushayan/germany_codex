using GermanyApplications.Api.Data.Configurations;
using GermanyApplications.Api.Data.Seed;
using GermanyApplications.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GermanyApplications.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, Role, Guid>(options)
{
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<AcademicQualification> AcademicQualifications => Set<AcademicQualification>();
    public DbSet<LanguageQualification> LanguageQualifications => Set<LanguageQualification>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<University> Universities => Set<University>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseVersion> CourseVersions => Set<CourseVersion>();
    public DbSet<CourseRequirement> CourseRequirements => Set<CourseRequirement>();
    public DbSet<CourseIntake> CourseIntakes => Set<CourseIntake>();
    public DbSet<ApplicationRoute> ApplicationRoutes => Set<ApplicationRoute>();
    public DbSet<Deadline> Deadlines => Set<Deadline>();
    public DbSet<SourceReference> SourceReferences => Set<SourceReference>();
    public DbSet<SavedCourse> SavedCourses => Set<SavedCourse>();
    public DbSet<EligibilityAssessment> EligibilityAssessments => Set<EligibilityAssessment>();
    public DbSet<EligibilityAssessmentItem> EligibilityAssessmentItems => Set<EligibilityAssessmentItem>();
    public DbSet<StudentApplication> StudentApplications => Set<StudentApplication>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistory => Set<ApplicationStatusHistory>();
    public DbSet<DocumentRequirement> DocumentRequirements => Set<DocumentRequirement>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("pg_trgm");
        builder.ApplyApplicationModel();
        DevelopmentSeed.Apply(builder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareTrackedEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareTrackedEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareTrackedEntities()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IImmutableEntity && entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException($"{entry.Metadata.ClrType.Name} records are immutable.");
            }

            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
            }

            if (entry.Entity is EntityBase entity)
            {
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = now;
                    entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = now;
                    entity.ConcurrencyToken = Guid.NewGuid();
                }
            }

            if (entry.Entity is User user && entry.State is EntityState.Added or EntityState.Modified)
            {
                user.CreatedAt = entry.State == EntityState.Added ? now : user.CreatedAt;
                user.UpdatedAt = now;
            }
            else if (entry.Entity is Role role && entry.State is EntityState.Added or EntityState.Modified)
            {
                role.CreatedAt = entry.State == EntityState.Added ? now : role.CreatedAt;
                role.UpdatedAt = now;
            }
        }
    }
}
