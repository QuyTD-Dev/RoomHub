using Application.DTOs.Auth;

namespace Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(LoginRequest request);

        Task<bool> RegisterAsync(RegisterRequest request);

        Task<bool> VerifyOtpAsync(VerifyOtpRequest request);

        Task<bool> CheckEmailExistsAsync(string email);

        Task<bool> ResendOtpAsync(string email);
    }
}