using System.Net;
using System.Net.Mail;
using System.Text;
using ClubHub.API.Services.Interfaces;

namespace ClubHub.API.Services.Implementations;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
        => _configuration = configuration;

    public async Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetToken)
    {
        var host = Required("Email:Smtp:Host");
        var port = int.TryParse(_configuration["Email:Smtp:Port"], out var configuredPort)
            ? configuredPort
            : 587;
        var username = Required("Email:Smtp:Username");
        var password = Required("Email:Smtp:Password");
        var configuredFromEmail = _configuration["Email:Smtp:FromEmail"];
        var fromEmail = string.IsNullOrWhiteSpace(configuredFromEmail) ? username : configuredFromEmail;
        var configuredFromName = _configuration["Email:Smtp:FromName"];
        var fromName = string.IsNullOrWhiteSpace(configuredFromName) ? "ClubHub" : configuredFromName;
        var useSsl = !bool.TryParse(_configuration["Email:Smtp:UseSsl"], out var configuredSsl) || configuredSsl;
        var resetPageUrl = Required("Email:ResetPasswordUrl");

        var separator = resetPageUrl.Contains('?') ? '&' : '?';
        var resetLink = $"{resetPageUrl}{separator}token={Uri.EscapeDataString(resetToken)}";
        var safeName = WebUtility.HtmlEncode(recipientName);
        var safeLink = WebUtility.HtmlEncode(resetLink);

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName, Encoding.UTF8),
            Subject = "Đặt lại mật khẩu ClubHub",
            Body = $"""
                <p>Xin chào {safeName},</p>
                <p>Bạn vừa yêu cầu đặt lại mật khẩu ClubHub.</p>
                <p><a href="{safeLink}">Đặt lại mật khẩu</a></p>
                <p>Liên kết có hiệu lực trong 60 phút. Nếu bạn không gửi yêu cầu này, hãy bỏ qua email.</p>
                """,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };
        message.To.Add(new MailAddress(recipientEmail, recipientName, Encoding.UTF8));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = useSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password)
        };

        await client.SendMailAsync(message);
    }

    private string Required(string key)
        => !string.IsNullOrWhiteSpace(_configuration[key])
            ? _configuration[key]!
            : throw new InvalidOperationException($"Missing required configuration: {key}");
}
