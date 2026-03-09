public interface IEmailService
{
    Task SendOtpEmailAsync(string email, string otp);
}