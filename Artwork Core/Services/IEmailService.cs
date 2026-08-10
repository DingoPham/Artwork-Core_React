namespace Artwork_Core.Services;

public interface IEmailService
{
    Task SendPasswordResetEmail(
        string email,
        string username,
        string resetLink);
}