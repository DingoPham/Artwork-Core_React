using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Artwork_Core.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendPasswordResetEmail(
        string email,
        string username,
        string resetLink)
    {
        var smtpHost = _config["Email:SmtpHost"];
        var smtpPort = int.Parse(
            _config["Email:SmtpPort"] ?? "587"
        );

        var smtpUsername = _config["Email:Username"];
        var smtpPassword = _config["Email:Password"];

        var fromEmail = _config["Email:FromEmail"];
        var fromName = _config["Email:FromName"] ?? "Artwork";

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(fromName, fromEmail)
        );

        message.To.Add(
            new MailboxAddress(username, email)
        );

        message.Subject = "Reset your password";

        var body = $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
        </head>

        <body style="
            margin: 0;
            padding: 40px 20px;
            background: #f5f5f5;
            font-family: Arial, sans-serif;
        ">

            <div style="
                max-width: 600px;
                margin: 0 auto;
                background: #ffffff;
                padding: 40px;
                border-radius: 12px;
            ">

                <h2 style="margin-top: 0;">
                    Reset your password
                </h2>

                <p>
                    Hello {username},
                </p>

                <p>
                    We received a request to reset your password.
                </p>

                <p>
                    Click the button below to create a new password.
                </p>

                <div style="margin: 30px 0;">
                    <a
                        href="{resetLink}"
                        style="
                            display: inline-block;
                            padding: 14px 24px;
                            background: #111111;
                            color: #ffffff;
                            text-decoration: none;
                            border-radius: 8px;
                        "
                    >
                        Reset password
                    </a>
                </div>

                <p>
                    This link will expire in 30 minutes.
                </p>

                <p>
                    If you did not request a password reset,
                    you can safely ignore this email.
                </p>

                <p style="color: #777;">
                    If the button doesn't work, copy and paste
                    this link into your browser:
                </p>

                <p style="
                    word-break: break-all;
                    color: #777;
                ">
                    {resetLink}
                </p>

            </div>

        </body>
        </html>
        """;

        message.Body = new BodyBuilder
        {
            HtmlBody = body
        }.ToMessageBody();

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            smtpHost,
            smtpPort,
            SecureSocketOptions.StartTls
        );

        await smtp.AuthenticateAsync(
            smtpUsername,
            smtpPassword
        );

        await smtp.SendAsync(message);

        await smtp.DisconnectAsync(true);
    }
}