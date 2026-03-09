using Application.DTOs.Auth;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // =========================
        // LOGIN
        // =========================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.LoginAsync(new LoginRequest
            {
                Email = model.Email,
                Password = model.Password,
                RememberMe = model.RememberMe
            });

            if (!result)
            {
                ModelState.AddModelError("", "Email or password is incorrect.");
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        // =========================
        // REGISTER
        // =========================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.RegisterAsync(new RegisterRequest
            {
                Email = model.Email,
                Password = model.Password,
                FullName = model.FullName,
                Role = model.Role
            });

            if (!result)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                return View(model);
            }

            // lưu email để verify OTP
            TempData["VerifyEmail"] = model.Email;

            return RedirectToAction(nameof(VerifyOtp));
        }

        // =========================
        // VERIFY OTP
        // =========================

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = TempData["VerifyEmail"]?.ToString();

            if (string.IsNullOrEmpty(email))
                return RedirectToAction(nameof(Register));

            var model = new OtpViewModel
            {
                Email = email
            };

            TempData.Keep("VerifyEmail");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(OtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var request = new VerifyOtpRequest
            {
                Email = model.Email,
                Code = model.Code
            };
            var result = await _authService.VerifyOtpAsync(request);

            if (!result)
            {
                ModelState.AddModelError("", "OTP không đúng hoặc đã hết hạn");
                return View(model);
            }

            return RedirectToAction("VerifySuccess");
        }
        public IActionResult VerifySuccess()
        {
            return View();
        }

        // =========================
        // VALIDATION
        // =========================

        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { exists = false });

            var exists = await _authService.CheckEmailExistsAsync(email);
            return Json(new { exists });
        }

        [HttpPost]
        public async Task<IActionResult> ResendOtp(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Email không hợp lệ." });

            var result = await _authService.ResendOtpAsync(email);

            if (result)
                return Json(new { success = true, message = "Mã OTP mới đã được gửi vào email của bạn." });
            
            return Json(new { success = false, message = "Phiên đăng ký đã hết hạn. Vui lòng đăng ký lại." });
        }
    }
}