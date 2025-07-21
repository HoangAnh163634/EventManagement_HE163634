using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventManagement.Models;
using Microsoft.AspNetCore.Http;
using EventManagement.Services;

namespace EventManagement.Pages.Account;

public class ProfileModel : PageModel
{
    private readonly EventManagementDbContext _context;
    private readonly ILogger<ProfileModel> _logger;
    private readonly AuthService _authService;

    public ProfileModel(
        EventManagementDbContext context,
        ILogger<ProfileModel> logger,
        AuthService authService)
    {
        _context = context;
        _logger = logger;
        _authService = authService;
    }

    [BindProperty]
    public User User { get; set; } = default!;
    public List<string> UserRoles { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/Account/Login");
        }

        var user = await _context.Users
            .Include(u => u.UserRoleUsers)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId.Value);

        if (user == null)
        {
            return NotFound();
        }

        User = user;
        UserRoles = user.UserRoleUsers
            .Where(ur => ur.IsActive)
            .Select(ur => ur.Role.RoleName)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/Account/Login");
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return NotFound();
        }

        if (await TryUpdateModelAsync(user, "User",
            u => u.FullName, u => u.PhoneNumber))
        {
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/Account/Login");
        }

        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin.";
            return RedirectToPage();
        }

        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] = "Mật khẩu mới không khớp.";
            return RedirectToPage();
        }

        var success = await _authService.ChangePasswordAsync(userId.Value, currentPassword, newPassword);
        if (!success)
        {
            TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng.";
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
        return RedirectToPage();
    }
} 