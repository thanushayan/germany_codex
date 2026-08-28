namespace GermanyApplications.Api.Email;

public sealed class DevelopmentAccountEmailSender(
    ILogger<DevelopmentAccountEmailSender> logger,
    IHostEnvironment environment) : IAccountEmailSender
{
    public Task SendEmailVerificationAsync(
        Guid userId,
        string email,
        string encodedCode,
        CancellationToken cancellationToken) =>
        RecordDeliveryAsync("email-verification", userId, email, encodedCode, cancellationToken);

    public Task SendPasswordResetAsync(
        Guid userId,
        string email,
        string encodedCode,
        CancellationToken cancellationToken) =>
        RecordDeliveryAsync("password-reset", userId, email, encodedCode, cancellationToken);

    private Task RecordDeliveryAsync(
        string messageType,
        Guid userId,
        string email,
        string encodedCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(encodedCode);
        cancellationToken.ThrowIfCancellationRequested();

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("A production account email provider must be configured.");
        }

        logger.LogInformation(
            "Development account email queued. MessageType={MessageType} UserId={UserId}",
            messageType,
            userId);
        return Task.CompletedTask;
    }
}
