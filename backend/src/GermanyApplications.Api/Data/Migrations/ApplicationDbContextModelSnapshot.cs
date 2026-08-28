using GermanyApplications.Api.Data.Configurations;
using GermanyApplications.Api.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace GermanyApplications.Api.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
public partial class ApplicationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyApplicationModel();
        DevelopmentSeed.Apply(modelBuilder);
    }
}
