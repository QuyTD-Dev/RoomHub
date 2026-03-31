using Infrastructure;
using Application.Interfaces.Services;
using Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using Web.Hubs;
using Application.Services;
using Domain.Entities;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddInfrastructure(builder.Configuration);

// Configure Identity cookie paths to match our Auth controller
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});


// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
//profile
builder.Services.AddScoped<IProfileService, ProfileService>();


// bật Session để lưu OTP
builder.Services.AddSession();
builder.Services.AddMemoryCache();

builder.Services.AddSignalR();

// Map UserIdentifier cho SignalR (Custom IUserIdProvider)
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();


var app = builder.Build();

// ============ AUTO-FIX: Reset mật khẩu seed bị lỗi hash + Fix roles ============
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var allUsers = userManager.Users.ToList();

    // Đảm bảo các role cần thiết tồn tại
    string[] roles = { "Admin", "PropertyOwner", "Tenant" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            Console.WriteLine($"[SeedFix] Đã tạo role: {role}");
        }
    }

    foreach (var user in allUsers)
    {
        // 1. Fix PasswordHash bị lỗi
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            try
            {
                var hashBytes = Convert.FromBase64String(user.PasswordHash);
                if (hashBytes.Length < 49)
                    throw new FormatException("Hash too short");
            }
            catch
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                await userManager.ResetPasswordAsync(user, token, "Pass@123");
                Console.WriteLine($"[SeedFix] Đã reset mật khẩu cho: {user.Email} → Pass@123");
            }
        }

        // 2. Fix Role: nếu user không có role nào → gán dựa theo dữ liệu
        var userRoles = await userManager.GetRolesAsync(user);
        if (!userRoles.Any())
        {
            // Mặc định gán Tenant, trừ khi email có gợi ý khác
            var email = user.Email?.ToLower() ?? "";
            if (email.Contains("admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
                Console.WriteLine($"[SeedFix] Đã gán role Admin cho: {user.Email}");
            }
            else
            {
                // Gán PropertyOwner cho các user có liên quan đến phòng/building
                await userManager.AddToRoleAsync(user, "PropertyOwner");
                Console.WriteLine($"[SeedFix] Đã gán role PropertyOwner cho: {user.Email}");
            }
        }
    }
}
// ====================================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// dùng session
app.UseSession();

// Identity
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=RoomPosts}/{action=Index}/{id?}");

app.MapHub<Web.Hubs.ChatHub>("/chathub");
app.MapHub<Web.Hubs.VideoCallHub>("/videocallhub");

app.Run();

