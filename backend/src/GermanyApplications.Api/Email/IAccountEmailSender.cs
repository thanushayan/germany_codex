namespace GermanyApplications.Api.Email;

public interface IAccountEmailSender
{
    Task SendEmailVerificationAsync(Guid userId, string email, string encodedCode, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(Guid userId, string email, string encodedCode, CancellationToken cancellationToken);
}
