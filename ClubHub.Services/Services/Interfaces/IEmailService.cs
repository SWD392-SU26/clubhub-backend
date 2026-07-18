namespace ClubHub.API.Services.Interfaces;

/// <summary>Service gửi email OTP</summary>
public interface IEmailService
{
    Task SendOtpAsync(string toEmail, string subject, string htmlBody);
}
