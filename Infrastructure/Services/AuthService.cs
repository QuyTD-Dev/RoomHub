using Application.DTOs.Auth;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _cache = cache;
        }

        // =========================
        // LOGIN
        // =========================

        public async Task<bool> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
                return false;

            // chưa verify email
            if (!user.EmailConfirmed)
                return false;

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                request.Password,
                request.RememberMe,
                lockoutOnFailure: true);

            return result.Succeeded;
        }

        // =========================
        // REGISTER
        // =========================

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);

            if (existingUser != null)
                return false;

            var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // Hash the password BEFORE saving to MemoryCache for security
            var dummyUser = new ApplicationUser { Email = request.Email, UserName = request.Email };
            var hashedPassword = _userManager.PasswordHasher.HashPassword(dummyUser, request.Password);

            var cacheData = new RegisterRequest
            {
                Email = request.Email,
                FullName = request.FullName,
                Password = hashedPassword, // Gán Hash Password vào đây
                Role = request.Role
            };

            // lưu info user tạm
            _cache.Set($"REGISTER_{request.Email}", cacheData, TimeSpan.FromMinutes(5));

            // lưu OTP
            _cache.Set($"OTP_{request.Email}", otp, TimeSpan.FromMinutes(5));

            await _emailService.SendOtpEmailAsync(request.Email, otp);

            return true;
        }

        // =========================
        // VERIFY OTP
        // =========================

        public async Task<bool> VerifyOtpAsync(VerifyOtpRequest request)
        {
            if (!_cache.TryGetValue($"OTP_{request.Email}", out string savedOtp))
                return false;

            if (savedOtp != request.Code)
                return false;

            if (!_cache.TryGetValue($"REGISTER_{request.Email}", out RegisterRequest registerData))
                return false;

            var user = new ApplicationUser
            {
                UserName = registerData.Email,
                Email = registerData.Email,
                FullName = registerData.FullName,
                EmailConfirmed = true,
                PasswordHash = registerData.Password // Lấy Hash Password từ Cache gán thẳng
            };

            // Tạo User mà không cần truyền Password dạng thô nữa
            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
                return false;

            await _userManager.AddToRoleAsync(user, registerData.Role);

            _cache.Remove($"OTP_{request.Email}");
            _cache.Remove($"REGISTER_{request.Email}");

            await _signInManager.SignInAsync(user, false);

            return true;
        }

        // =========================
        // VALIDATION
        // =========================

        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null;
        }

        // =========================
        // RESEND OTP
        // =========================

        public async Task<bool> ResendOtpAsync(string email)
        {
            // Kiểm tra xem session Register có còn tồn tại không
            if (!_cache.TryGetValue($"REGISTER_{email}", out RegisterRequest _))
                return false;

            // Generate new OTP
            var newOtp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // Cập nhật lại thời gian sống của OTP (5 phút)
            _cache.Set($"OTP_{email}", newOtp, TimeSpan.FromMinutes(5));

            // Gửi lại email
            await _emailService.SendOtpEmailAsync(email, newOtp);

            return true;
        }
    }
}