using System.Net;
using System.Net.Mail;
using ClubHub.API.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClubHub.API.Services.Implementations;

/// <summary>Gửi email OTP qua SMTP (Gmail / SendGrid etc.)</summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;

    public SmtpEmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendOtpAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _config["Email:SmtpHost"]!;
        var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var user = _config["Email:Username"]!;
        var pass = _config["Email:Password"]!;
        var from = _config["Email:From"] ?? user;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass)
        };

        var message = new MailMessage
        {
            From = new MailAddress(from, "ClubHub"),
            Subject = subject,
            Body = $"""
                <html><body style="font-family:Arial,sans-serif;padding:20px">
                  <h2 style="color:#4F46E5">ClubHub</h2>
                  <p>{htmlBody}</p>
                  <p style="color:#6B7280;font-size:12px">Email này được gửi tự động, vui lòng không trả lời.</p>
                </body></html>
                """,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }
}
