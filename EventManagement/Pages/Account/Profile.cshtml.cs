using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventManagement.Models;
using EventManagement.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EventManagement.Pages.Account;

public class ProfileModel : PageModel
{
    private readonly EventManagementDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(
        EventManagementDbContext context,
        EmailService emailService,
        ILogger<ProfileModel> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    public User User { get; set; } = default!;
    public List<string> UserRoles { get; set; } = new();
    public int AttendedEventsCount { get; set; }
    public int OrganizedEventsCount { get; set; }
    public int FeedbackCount { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var user = await _context.Users
            .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound();
        }

        User = user;
        UserRoles = user.UserRoleUsers
            .Where(ur => ur.IsActive)
            .Select(ur => ur.Role.RoleName)
            .ToList();

        // Lấy thống kê
        AttendedEventsCount = await _context.Registrations
            .CountAsync(r => r.AttendeeId == userId && r.Status == "Attended");

        OrganizedEventsCount = await _context.Events
            .CountAsync(e => e.OrganizerId == userId && !e.IsDeleted);

        FeedbackCount = await _context.Feedbacks
            .CountAsync(f => f.AttendeeId == userId);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (await TryUpdateModelAsync(user, "User",
            u => u.FullName,
            u => u.PhoneNumber,
            u => u.DateOfBirth,
            u => u.Gender,
            u => u.Address))
        {
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            SuccessMessage = "Thông tin cá nhân đã được cập nhật.";
            return RedirectToPage();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (newPassword != confirmPassword)
        {
            ErrorMessage = "Mật khẩu mới không khớp.";
            return RedirectToPage();
        }

        var currentPasswordHash = HashPassword(currentPassword);
        if (currentPasswordHash != user.PasswordHash)
        {
            ErrorMessage = "Mật khẩu hiện tại không đúng.";
            return RedirectToPage();
        }

        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        SuccessMessage = "Mật khẩu đã được thay đổi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResendVerificationAsync()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (user.IsEmailVerified)
        {
            ErrorMessage = "Email đã được xác thực.";
            return RedirectToPage();
        }

        // Tạo token mới
        user.EmailVerificationToken = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Gửi email xác thực
        var verificationUrl = $"{Request.Scheme}://{Request.Host}/Account/VerifyEmail?token={user.EmailVerificationToken}";
        var emailBody = $@"<p>Chào {user.FullName},</p>
<p>Vui lòng click vào link bên dưới để xác thực email của bạn:</p>
<p><a href='{verificationUrl}'>{verificationUrl}</a></p>
<p>Link xác thực này sẽ hết hạn sau 24 giờ.</p>
<p>Trân trọng,<br>Ban quản trị</p>";

        await _emailService.SendEmailAsync(
            user.Email,
            "Xác thực email",
            emailBody,
            user.FullName);

        SuccessMessage = "Email xác thực đã được gửi lại.";
        return RedirectToPage();
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
} 