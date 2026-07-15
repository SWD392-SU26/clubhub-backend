namespace ClubHub.API.Services.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetToken);
}
