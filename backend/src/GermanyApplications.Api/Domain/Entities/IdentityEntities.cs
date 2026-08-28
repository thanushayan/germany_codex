using Microsoft.AspNetCore.Identity;

namespace GermanyApplications.Api.Domain.Entities;

public sealed class User : IdentityUser<Guid>, ISoftDeletable
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public StudentProfile? StudentProfile { get; set; }
}

public sealed class Role : IdentityRole<Guid>
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
