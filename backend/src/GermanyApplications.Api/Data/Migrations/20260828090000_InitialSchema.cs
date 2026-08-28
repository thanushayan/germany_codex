using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GermanyApplications.Api.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260828090000_InitialSchema")]
public partial class InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        migrationBuilder.Sql(InitialSchemaSql);
        migrationBuilder.Sql(PublicationGuardSql);
        migrationBuilder.Sql(ImmutableHistoryGuardSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS prevent_historical_mutation() CASCADE;");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS enforce_official_course_source() CASCADE;");
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS "user_tokens", "user_roles", "user_logins", "user_claims", "role_claims",
              "support_tickets", "audit_logs", "consent_records", "notifications", "student_documents",
              "application_status_history", "student_applications", "eligibility_assessment_items",
              "eligibility_assessments", "saved_courses", "document_requirements", "deadlines",
              "application_routes", "course_intakes", "course_requirements", "course_versions",
              "source_references", "courses", "universities", "work_experiences", "language_qualifications",
              "academic_qualifications", "student_profiles", "roles", "users" CASCADE;
            """);
    }


    private const string PublicationGuardSql = """
        CREATE FUNCTION enforce_official_course_source() RETURNS trigger AS $$
        BEGIN
          IF NEW."Status" = 'Published' AND NOT EXISTS (
            SELECT 1 FROM "source_references" source
            WHERE source."Id" = NEW."OfficialSourceReferenceId"
              AND source."IsOfficial" = true
              AND source."CourseId" = NEW."CourseId"
              AND source."Url" ~ '^https://'
              AND source."VerifiedAt" IS NOT NULL
          ) THEN
            RAISE EXCEPTION 'Published course versions require a verified official HTTPS source reference';
          END IF;
          RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;
        CREATE CONSTRAINT TRIGGER "trg_course_version_official_source"
          AFTER INSERT OR UPDATE OF "Status", "OfficialSourceReferenceId", "VerifiedAt", "PublishedAt"
          ON "course_versions" DEFERRABLE INITIALLY IMMEDIATE
          FOR EACH ROW EXECUTE FUNCTION enforce_official_course_source();
        """;

    private const string ImmutableHistoryGuardSql = """
        CREATE FUNCTION prevent_historical_mutation() RETURNS trigger AS $$
        BEGIN
          RAISE EXCEPTION '% is append-only; create a new historical record instead', TG_TABLE_NAME;
        END;
        $$ LANGUAGE plpgsql;
        CREATE TRIGGER "trg_eligibility_assessments_immutable" BEFORE UPDATE OR DELETE ON "eligibility_assessments" FOR EACH ROW EXECUTE FUNCTION prevent_historical_mutation();
        CREATE TRIGGER "trg_eligibility_assessment_items_immutable" BEFORE UPDATE OR DELETE ON "eligibility_assessment_items" FOR EACH ROW EXECUTE FUNCTION prevent_historical_mutation();
        CREATE TRIGGER "trg_application_status_history_immutable" BEFORE UPDATE OR DELETE ON "application_status_history" FOR EACH ROW EXECUTE FUNCTION prevent_historical_mutation();
        CREATE TRIGGER "trg_consent_records_immutable" BEFORE UPDATE OR DELETE ON "consent_records" FOR EACH ROW EXECUTE FUNCTION prevent_historical_mutation();
        CREATE TRIGGER "trg_audit_logs_immutable" BEFORE UPDATE OR DELETE ON "audit_logs" FOR EACH ROW EXECUTE FUNCTION prevent_historical_mutation();
        """;

    private const string InitialSchemaSql = """
        CREATE TABLE "users" (
          "Id" uuid PRIMARY KEY, "UserName" varchar(256), "NormalizedUserName" varchar(256),
          "Email" varchar(256), "NormalizedEmail" varchar(256), "EmailConfirmed" boolean NOT NULL,
          "PasswordHash" text, "SecurityStamp" text, "ConcurrencyStamp" text,
          "PhoneNumber" text, "PhoneNumberConfirmed" boolean NOT NULL, "TwoFactorEnabled" boolean NOT NULL,
          "LockoutEnd" timestamptz, "LockoutEnabled" boolean NOT NULL, "AccessFailedCount" integer NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL,
          "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz
        );
        CREATE UNIQUE INDEX "UserNameIndex" ON "users" ("NormalizedUserName") WHERE "NormalizedUserName" IS NOT NULL;
        CREATE INDEX "EmailIndex" ON "users" ("NormalizedEmail");
        CREATE INDEX "IX_users_IsDeleted_CreatedAt" ON "users" ("IsDeleted", "CreatedAt");

        CREATE TABLE "roles" (
          "Id" uuid PRIMARY KEY, "Name" varchar(256), "NormalizedName" varchar(256), "ConcurrencyStamp" text,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL
        );
        CREATE UNIQUE INDEX "RoleNameIndex" ON "roles" ("NormalizedName") WHERE "NormalizedName" IS NOT NULL;
        CREATE TABLE "role_claims" ("Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, "RoleId" uuid NOT NULL REFERENCES "roles"("Id") ON DELETE RESTRICT, "ClaimType" text, "ClaimValue" text);
        CREATE TABLE "user_claims" ("Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT, "ClaimType" text, "ClaimValue" text);
        CREATE TABLE "user_logins" ("LoginProvider" text NOT NULL, "ProviderKey" text NOT NULL, "ProviderDisplayName" text, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT, PRIMARY KEY ("LoginProvider", "ProviderKey"));
        CREATE TABLE "user_roles" ("UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT, "RoleId" uuid NOT NULL REFERENCES "roles"("Id") ON DELETE RESTRICT, PRIMARY KEY ("UserId", "RoleId"));
        CREATE TABLE "user_tokens" ("UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT, "LoginProvider" text NOT NULL, "Name" text NOT NULL, "Value" text, PRIMARY KEY ("UserId", "LoginProvider", "Name"));
        CREATE INDEX "IX_role_claims_RoleId" ON "role_claims" ("RoleId"); CREATE INDEX "IX_user_claims_UserId" ON "user_claims" ("UserId");
        CREATE INDEX "IX_user_logins_UserId" ON "user_logins" ("UserId"); CREATE INDEX "IX_user_roles_RoleId" ON "user_roles" ("RoleId");

        CREATE TABLE "student_profiles" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL UNIQUE REFERENCES "users"("Id") ON DELETE RESTRICT,
          "PreferredLocale" varchar(10), "CitizenshipCountryCode" varchar(2), "ResidenceCountryCode" varchar(2),
          "ExpectedStudyStartDate" date, "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL,
          UNIQUE ("Id", "UserId")
        );
        CREATE TABLE "academic_qualifications" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT,
          "StudentProfileId" uuid NOT NULL,
          "QualificationType" varchar(100) NOT NULL, "InstitutionName" varchar(250) NOT NULL, "CountryCode" varchar(2) NOT NULL,
          "SubjectArea" varchar(200) NOT NULL, "GradingSystem" varchar(100), "FinalGrade" numeric(10,4), "Credits" numeric(10,2),
          "CreditSystem" varchar(50), "GraduationDate" date, "IsCompleted" boolean NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL,
          FOREIGN KEY ("StudentProfileId", "UserId") REFERENCES "student_profiles"("Id", "UserId") ON DELETE RESTRICT
        );
        CREATE TABLE "language_qualifications" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT,
          "StudentProfileId" uuid NOT NULL,
          "LanguageCode" varchar(10) NOT NULL, "TestName" varchar(100) NOT NULL, "OverallScore" numeric(10,2),
          "ScoreScale" varchar(50), "TestDate" date, "ValidUntil" date,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL,
          FOREIGN KEY ("StudentProfileId", "UserId") REFERENCES "student_profiles"("Id", "UserId") ON DELETE RESTRICT
        );
        CREATE TABLE "work_experiences" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT,
          "StudentProfileId" uuid NOT NULL,
          "EmployerName" varchar(250) NOT NULL, "JobTitle" varchar(200) NOT NULL, "Industry" varchar(150),
          "StartDate" date NOT NULL, "EndDate" date, "Description" varchar(2000),
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL,
          CONSTRAINT "ck_work_experience_dates" CHECK ("EndDate" IS NULL OR "EndDate" >= "StartDate"),
          FOREIGN KEY ("StudentProfileId", "UserId") REFERENCES "student_profiles"("Id", "UserId") ON DELETE RESTRICT
        );
        CREATE INDEX "IX_academic_qualifications_UserId" ON "academic_qualifications" ("UserId"); CREATE INDEX "IX_academic_qualifications_StudentProfileId" ON "academic_qualifications" ("StudentProfileId");
        CREATE INDEX "IX_language_qualifications_UserId" ON "language_qualifications" ("UserId"); CREATE INDEX "IX_language_qualifications_StudentProfileId" ON "language_qualifications" ("StudentProfileId");
        CREATE INDEX "IX_work_experiences_UserId" ON "work_experiences" ("UserId"); CREATE INDEX "IX_work_experiences_StudentProfileId" ON "work_experiences" ("StudentProfileId");

        CREATE TABLE "universities" (
          "Id" uuid PRIMARY KEY, "Name" varchar(250) NOT NULL, "City" varchar(150), "CountryCode" varchar(2) NOT NULL,
          "OfficialWebsiteUrl" varchar(2048), "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_universities_CountryCode_Name" ON "universities" ("CountryCode", "Name");
        CREATE TABLE "courses" (
          "Id" uuid PRIMARY KEY, "UniversityId" uuid NOT NULL REFERENCES "universities"("Id") ON DELETE RESTRICT,
          "StableCode" varchar(100) NOT NULL, "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL,
          UNIQUE ("UniversityId", "StableCode")
        );
        CREATE INDEX "IX_courses_IsDeleted_UniversityId" ON "courses" ("IsDeleted", "UniversityId");
        CREATE TABLE "source_references" (
          "Id" uuid PRIMARY KEY, "UniversityId" uuid REFERENCES "universities"("Id") ON DELETE RESTRICT,
          "CourseId" uuid REFERENCES "courses"("Id") ON DELETE RESTRICT, "Url" varchar(2048) NOT NULL,
          "Title" varchar(500) NOT NULL, "IsOfficial" boolean NOT NULL, "VerifiedAt" timestamptz NOT NULL, "VerifiedBy" varchar(200),
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_source_references_Url" ON "source_references" ("Url");
        CREATE INDEX "IX_source_references_CourseId_IsOfficial_VerifiedAt" ON "source_references" ("CourseId", "IsOfficial", "VerifiedAt");
        CREATE TABLE "course_versions" (
          "Id" uuid PRIMARY KEY, "CourseId" uuid NOT NULL REFERENCES "courses"("Id") ON DELETE RESTRICT,
          "VersionNumber" integer NOT NULL, "Title" varchar(300) NOT NULL, "Discipline" varchar(150) NOT NULL,
          "DegreeAward" varchar(100) NOT NULL, "TeachingLanguage" varchar(100) NOT NULL, "Summary" varchar(4000),
          "Status" varchar(30) NOT NULL, "IsDevelopmentSample" boolean NOT NULL DEFAULT false,
          "OfficialSourceReferenceId" uuid REFERENCES "source_references"("Id") ON DELETE RESTRICT,
          "VerifiedAt" timestamptz, "PublishedAt" timestamptz,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL,
          UNIQUE ("CourseId", "VersionNumber"),
          CONSTRAINT "ck_course_version_publication_source" CHECK ("Status" <> 'Published' OR ("OfficialSourceReferenceId" IS NOT NULL AND "VerifiedAt" IS NOT NULL AND "PublishedAt" IS NOT NULL AND NOT "IsDevelopmentSample"))
        );
        CREATE INDEX "IX_course_versions_Status_Discipline_TeachingLanguage" ON "course_versions" ("Status", "Discipline", "TeachingLanguage");
        CREATE INDEX "IX_course_versions_Title_trgm" ON "course_versions" USING gin ("Title" gin_trgm_ops);
        CREATE TABLE "course_requirements" (
          "Id" uuid PRIMARY KEY, "CourseVersionId" uuid NOT NULL REFERENCES "course_versions"("Id") ON DELETE RESTRICT,
          "Type" varchar(50) NOT NULL, "Operator" varchar(50) NOT NULL, "Name" varchar(250) NOT NULL,
          "SubjectArea" varchar(200), "NumericValue" numeric(18,4), "TextValue" varchar(1000), "BooleanValue" boolean,
          "Unit" varchar(100), "HumanReadableDescription" varchar(2000), "SourceReferenceId" uuid NOT NULL REFERENCES "source_references"("Id") ON DELETE RESTRICT,
          "IsMandatory" boolean NOT NULL, "SortOrder" integer NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL,
          CONSTRAINT "ck_course_requirement_typed_value" CHECK ("NumericValue" IS NOT NULL OR "TextValue" IS NOT NULL OR "BooleanValue" IS NOT NULL OR "Operator" = 'Informational')
        );
        CREATE INDEX "IX_course_requirements_CourseVersionId_Type_SortOrder" ON "course_requirements" ("CourseVersionId", "Type", "SortOrder");
        CREATE TABLE "course_intakes" (
          "Id" uuid PRIMARY KEY, "CourseVersionId" uuid NOT NULL REFERENCES "course_versions"("Id") ON DELETE RESTRICT,
          "Term" varchar(20) NOT NULL, "Year" integer, "Label" varchar(100), "StudyStartDate" date,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_course_intakes_CourseVersionId_Term_Year" ON "course_intakes" ("CourseVersionId", "Term", "Year");
        CREATE TABLE "application_routes" (
          "Id" uuid PRIMARY KEY, "CourseVersionId" uuid NOT NULL REFERENCES "course_versions"("Id") ON DELETE RESTRICT,
          "Type" varchar(50) NOT NULL, "Name" varchar(200) NOT NULL, "OfficialApplicationUrl" varchar(2048) NOT NULL,
          "SourceReferenceId" uuid NOT NULL REFERENCES "source_references"("Id") ON DELETE RESTRICT,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_application_routes_CourseVersionId_Type" ON "application_routes" ("CourseVersionId", "Type");
        CREATE TABLE "deadlines" (
          "Id" uuid PRIMARY KEY, "CourseVersionId" uuid NOT NULL REFERENCES "course_versions"("Id") ON DELETE RESTRICT,
          "CourseIntakeId" uuid REFERENCES "course_intakes"("Id") ON DELETE RESTRICT,
          "ApplicationRouteId" uuid REFERENCES "application_routes"("Id") ON DELETE RESTRICT,
          "DeadlineType" varchar(100) NOT NULL, "ApplicantCategory" varchar(150) NOT NULL,
          "DeadlineDate" date NOT NULL, "DeadlineTime" time, "TimeZoneId" varchar(100) NOT NULL,
          "SourceReferenceId" uuid NOT NULL REFERENCES "source_references"("Id") ON DELETE RESTRICT, "VerifiedAt" timestamptz NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_deadlines_DeadlineDate_ApplicantCategory" ON "deadlines" ("DeadlineDate", "ApplicantCategory");
        CREATE INDEX "IX_deadlines_CourseVersionId_CourseIntakeId" ON "deadlines" ("CourseVersionId", "CourseIntakeId");
        CREATE TABLE "document_requirements" (
          "Id" uuid PRIMARY KEY, "CourseVersionId" uuid NOT NULL REFERENCES "course_versions"("Id") ON DELETE RESTRICT,
          "DocumentType" varchar(100) NOT NULL, "Name" varchar(250) NOT NULL, "Description" varchar(2000), "IsMandatory" boolean NOT NULL,
          "SourceReferenceId" uuid NOT NULL REFERENCES "source_references"("Id") ON DELETE RESTRICT, "SortOrder" integer NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_document_requirements_CourseVersionId_SortOrder" ON "document_requirements" ("CourseVersionId", "SortOrder");

        CREATE TABLE "saved_courses" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT,
          "CourseId" uuid NOT NULL REFERENCES "courses"("Id") ON DELETE RESTRICT,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL,
          UNIQUE ("UserId", "CourseId")
        );
        CREATE INDEX "IX_saved_courses_UserId_CreatedAt" ON "saved_courses" ("UserId", "CreatedAt");
        CREATE TABLE "eligibility_assessments" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT,
          "CourseVersionId" uuid NOT NULL REFERENCES "course_versions"("Id") ON DELETE RESTRICT,
          "Outcome" varchar(50) NOT NULL, "AssessedAt" timestamptz NOT NULL, "InputSnapshotJson" jsonb NOT NULL,
          "RuleSetVersion" varchar(100) NOT NULL, "DisclaimerAcknowledgedAt" timestamptz NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_eligibility_assessments_UserId_AssessedAt" ON "eligibility_assessments" ("UserId", "AssessedAt");
        CREATE INDEX "IX_eligibility_assessments_CourseVersionId_Outcome" ON "eligibility_assessments" ("CourseVersionId", "Outcome");
        CREATE TABLE "eligibility_assessment_items" (
          "Id" uuid PRIMARY KEY, "EligibilityAssessmentId" uuid NOT NULL REFERENCES "eligibility_assessments"("Id") ON DELETE RESTRICT,
          "CourseRequirementId" uuid REFERENCES "course_requirements"("Id") ON DELETE RESTRICT, "Result" varchar(50) NOT NULL,
          "RequirementSnapshotJson" jsonb NOT NULL, "StudentValueSnapshot" varchar(1000), "Explanation" varchar(2000) NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_eligibility_assessment_items_EligibilityAssessmentId_Result" ON "eligibility_assessment_items" ("EligibilityAssessmentId", "Result");
        CREATE TABLE "student_applications" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT,
          "CourseVersionId" uuid NOT NULL REFERENCES "course_versions"("Id") ON DELETE RESTRICT,
          "CourseIntakeId" uuid REFERENCES "course_intakes"("Id") ON DELETE RESTRICT, "ApplicationRouteId" uuid REFERENCES "application_routes"("Id") ON DELETE RESTRICT,
          "Status" varchar(50) NOT NULL, "ExternalReference" varchar(250), "SubmittedByStudentAt" timestamptz,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL,
          UNIQUE ("Id", "UserId")
        );
        CREATE INDEX "IX_student_applications_UserId_Status_UpdatedAt" ON "student_applications" ("UserId", "Status", "UpdatedAt");
        CREATE INDEX "IX_student_applications_CourseVersionId_Status" ON "student_applications" ("CourseVersionId", "Status");
        CREATE TABLE "application_status_history" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT,
          "StudentApplicationId" uuid NOT NULL,
          "FromStatus" varchar(50) NOT NULL, "ToStatus" varchar(50) NOT NULL, "ChangedAt" timestamptz NOT NULL, "Note" varchar(2000),
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL,
          FOREIGN KEY ("StudentApplicationId", "UserId") REFERENCES "student_applications"("Id", "UserId") ON DELETE RESTRICT
        );
        CREATE INDEX "IX_application_status_history_UserId_StudentApplicationId_ChangedAt" ON "application_status_history" ("UserId", "StudentApplicationId", "ChangedAt");
        CREATE TABLE "student_documents" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT,
          "StudentApplicationId" uuid,
          "DocumentRequirementId" uuid REFERENCES "document_requirements"("Id") ON DELETE RESTRICT,
          "DocumentType" varchar(100) NOT NULL, "DisplayName" varchar(250) NOT NULL, "OriginalFileName" varchar(255),
          "MediaType" varchar(150), "SizeBytes" bigint, "Sha256Checksum" varchar(64), "Status" varchar(50) NOT NULL,
          "IsDeleted" boolean NOT NULL DEFAULT false, "DeletedAt" timestamptz,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL,
          FOREIGN KEY ("StudentApplicationId", "UserId") REFERENCES "student_applications"("Id", "UserId") ON DELETE RESTRICT
        );
        CREATE INDEX "IX_student_documents_UserId_Status_UpdatedAt" ON "student_documents" ("UserId", "Status", "UpdatedAt");
        CREATE INDEX "IX_student_documents_StudentApplicationId" ON "student_documents" ("StudentApplicationId");
        CREATE TABLE "notifications" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT,
          "Type" varchar(50) NOT NULL, "Status" varchar(50) NOT NULL, "Locale" varchar(10) NOT NULL,
          "SubjectKey" varchar(200) NOT NULL, "BodyKey" varchar(200) NOT NULL, "TemplateDataJson" jsonb NOT NULL,
          "ScheduledFor" timestamptz, "SentAt" timestamptz, "ReadAt" timestamptz, "IdempotencyKey" varchar(200) NOT NULL UNIQUE,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_notifications_UserId_Status_ScheduledFor" ON "notifications" ("UserId", "Status", "ScheduledFor");
        CREATE TABLE "consent_records" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT,
          "ConsentType" varchar(50) NOT NULL, "PolicyVersion" varchar(100) NOT NULL, "Granted" boolean NOT NULL,
          "RecordedAt" timestamptz NOT NULL, "Locale" varchar(10) NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_consent_records_UserId_ConsentType_RecordedAt" ON "consent_records" ("UserId", "ConsentType", "RecordedAt");
        CREATE TABLE "audit_logs" (
          "Id" uuid PRIMARY KEY, "ActorUserId" uuid REFERENCES "users"("Id") ON DELETE RESTRICT,
          "Action" varchar(200) NOT NULL, "TargetType" varchar(200) NOT NULL, "TargetId" uuid, "Outcome" varchar(50) NOT NULL,
          "CorrelationId" varchar(100), "MetadataJson" jsonb, "OccurredAt" timestamptz NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_audit_logs_OccurredAt" ON "audit_logs" ("OccurredAt");
        CREATE INDEX "IX_audit_logs_ActorUserId_OccurredAt" ON "audit_logs" ("ActorUserId", "OccurredAt");
        CREATE INDEX "IX_audit_logs_TargetType_TargetId_OccurredAt" ON "audit_logs" ("TargetType", "TargetId", "OccurredAt");
        CREATE TABLE "support_tickets" (
          "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES "users"("Id") ON DELETE RESTRICT,
          "Status" varchar(50) NOT NULL, "Priority" varchar(50) NOT NULL, "Subject" varchar(250) NOT NULL,
          "Description" varchar(5000) NOT NULL, "ResolvedAt" timestamptz,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "ConcurrencyToken" uuid NOT NULL
        );
        CREATE INDEX "IX_support_tickets_UserId_Status_UpdatedAt" ON "support_tickets" ("UserId", "Status", "UpdatedAt");
        CREATE INDEX "IX_support_tickets_Status_Priority_CreatedAt" ON "support_tickets" ("Status", "Priority", "CreatedAt");

        INSERT INTO "roles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp", "CreatedAt", "UpdatedAt") VALUES
          ('10000000-0000-0000-0000-000000000001', 'Student', 'STUDENT', '10000000000000000000000000000001', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z'),
          ('10000000-0000-0000-0000-000000000002', 'ContentAuthor', 'CONTENTAUTHOR', '10000000000000000000000000000002', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z'),
          ('10000000-0000-0000-0000-000000000003', 'ContentReviewer', 'CONTENTREVIEWER', '10000000000000000000000000000003', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z'),
          ('10000000-0000-0000-0000-000000000004', 'PrivacySupport', 'PRIVACYSUPPORT', '10000000000000000000000000000004', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z'),
          ('10000000-0000-0000-0000-000000000005', 'SecurityAdministrator', 'SECURITYADMINISTRATOR', '10000000000000000000000000000005', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z');
        INSERT INTO "universities" ("Id", "Name", "City", "CountryCode", "IsDeleted", "CreatedAt", "UpdatedAt", "ConcurrencyToken") VALUES
          ('20000000-0000-0000-0000-000000000001', '[SYNTHETIC DEVELOPMENT DATA] Example German University', 'Example City', 'DE', false, '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', '21000000-0000-0000-0000-000000000001');
        INSERT INTO "courses" ("Id", "UniversityId", "StableCode", "IsDeleted", "CreatedAt", "UpdatedAt", "ConcurrencyToken") VALUES
          ('30000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'DEV-SYNTHETIC-CS', false, '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', '30000000-0000-0000-0000-000000000001'),
          ('30000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001', 'DEV-SYNTHETIC-DS', false, '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', '30000000-0000-0000-0000-000000000002');
        INSERT INTO "course_versions" ("Id", "CourseId", "VersionNumber", "Title", "Discipline", "DegreeAward", "TeachingLanguage", "Summary", "Status", "IsDevelopmentSample", "CreatedAt", "UpdatedAt", "ConcurrencyToken") VALUES
          ('31000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 1, '[SYNTHETIC DEVELOPMENT DATA] Example Computer Science MSc', 'Computer Science', 'Master of Science', 'English', 'Synthetic draft used only to verify development catalogue plumbing. It is not a real programme.', 'Draft', true, '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', '31000000-0000-0000-0000-000000000001'),
          ('31000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000002', 1, '[SYNTHETIC DEVELOPMENT DATA] Example Data Science MSc', 'Data Science', 'Master of Science', 'English', 'Synthetic draft used only to verify development catalogue plumbing. It is not a real programme.', 'Draft', true, '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', '31000000-0000-0000-0000-000000000002');
        """;
}
