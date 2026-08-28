using GermanyApplications.Api.Data;
using GermanyApplications.Api.Domain.Entities;

namespace GermanyApplications.Api.Authentication;

public interface ISecurityAuditWriter
{
    Task WriteAsync(Guid? actorUserId, string action, string outcome, HttpContext context, CancellationToken cancellationToken);
}

public sealed class SecurityAuditWriter(ApplicationDbContext dbContext) : ISecurityAuditWriter
{
    public async Task WriteAsync(
        Guid? actorUserId,
        string action,
        string outcome,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            TargetType = "UserAccount",
            TargetId = actorUserId,
            Outcome = outcome,
            CorrelationId = context.TraceIdentifier,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
