using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GermanyApplications.Api.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260828091000_AlignAuthenticationRoles")]
public partial class AlignAuthenticationRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "roles" SET "Name" = 'ContentEditor', "NormalizedName" = 'CONTENTEDITOR'
              WHERE "Id" = '10000000-0000-0000-0000-000000000002';
            UPDATE "roles" SET "Name" = 'Reviewer', "NormalizedName" = 'REVIEWER'
              WHERE "Id" = '10000000-0000-0000-0000-000000000003';
            UPDATE "roles" SET "Name" = 'SupportAgent', "NormalizedName" = 'SUPPORTAGENT'
              WHERE "Id" = '10000000-0000-0000-0000-000000000004';
            UPDATE "roles" SET "Name" = 'Admin', "NormalizedName" = 'ADMIN'
              WHERE "Id" = '10000000-0000-0000-0000-000000000005';
            INSERT INTO "roles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp", "CreatedAt", "UpdatedAt")
              VALUES ('10000000-0000-0000-0000-000000000006', 'SuperAdmin', 'SUPERADMIN',
                '10000000000000000000000000000006', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "roles" WHERE "Id" = '10000000-0000-0000-0000-000000000006';
            UPDATE "roles" SET "Name" = 'ContentAuthor', "NormalizedName" = 'CONTENTAUTHOR'
              WHERE "Id" = '10000000-0000-0000-0000-000000000002';
            UPDATE "roles" SET "Name" = 'ContentReviewer', "NormalizedName" = 'CONTENTREVIEWER'
              WHERE "Id" = '10000000-0000-0000-0000-000000000003';
            UPDATE "roles" SET "Name" = 'PrivacySupport', "NormalizedName" = 'PRIVACYSUPPORT'
              WHERE "Id" = '10000000-0000-0000-0000-000000000004';
            UPDATE "roles" SET "Name" = 'SecurityAdministrator', "NormalizedName" = 'SECURITYADMINISTRATOR'
              WHERE "Id" = '10000000-0000-0000-0000-000000000005';
            """);
    }
}
