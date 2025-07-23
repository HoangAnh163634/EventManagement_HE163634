using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventManagement.Pages.Admin;

public class UserDetailsModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly EmailService _emailService;
    private readonly ILogger<UserDetailsModel> _logger;

    public UserDetailsModel(
        AdminService adminService,
        EmailService emailService,
        ILogger<UserDetailsModel> logger)
    {
        _adminService = adminService;
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    public new User User { get; set; } = new();
    public List<Role> AllRoles { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            // Kiểm tra phân quyền
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToPage("/Index");
            }

            // Lấy thông tin user và danh sách role
            User = await _adminService.GetUserByIdAsync(id, false) ?? new User();
            if (User == null)
            {
                return Page();
            }

            AllRoles = await _adminService.GetAllRolesAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user details for user {UserId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin người dùng.";
            return RedirectToPage("/Admin/Users");
        }
    }

    public async Task<IActionResult> OnPostAsync(int[] selectedRoles)
    {
        try
        {
            // Kiểm tra phân quyền
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            // Cập nhật thông tin user
            await _adminService.UpdateUserAsync(User.UserId, User.IsActive, selectedRoles);
            
            // Ghi log
            _logger.LogInformation(
                "User {UserId} updated by admin. Status: {IsActive}, Roles: {Roles}",
                User.UserId,
                User.IsActive,
                string.Join(", ", selectedRoles));

            TempData["SuccessMessage"] = "Cập nhật người dùng thành công.";
            return RedirectToPage("/Admin/UserDetails", new { id = User.UserId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", User.UserId);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật người dùng.";
            return RedirectToPage("/Admin/UserDetails", new { id = User.UserId });
        }
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(int id)
    {
        try
        {
            // Kiểm tra phân quyền
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            // Reset mật khẩu
            var (success, newPassword) = await _adminService.ResetUserPasswordAsync(id);
            if (!success)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy người dùng." });
            }

            // Gửi email thông báo
            var user = await _adminService.GetUserByIdAsync(id, false);
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy người dùng." });
            }
            var emailBody = $@"<p>Chào {user.FullName},</p>
<p>Mật khẩu của bạn đã được reset bởi Admin.</p>
<p>Mật khẩu mới của bạn là: <strong>{newPassword}</strong></p>
<p>Vui lòng đăng nhập và đổi mật khẩu ngay.</p>
<p>Trân trọng,<br>Ban quản trị</p>";

            await _emailService.SendEmailAsync(
                user.Email,
                "Reset mật khẩu",
                emailBody,
                user.FullName);

            // Ghi log
            _logger.LogInformation("Password reset for user {UserId} by admin", id);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", id);
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi reset mật khẩu." });
        }
    }

    public async Task<IActionResult> OnPostResendVerificationAsync(int id)
    {
        try
        {
            // Kiểm tra phân quyền
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            // Gửi lại email xác thực
            var success = await _adminService.ResendVerificationEmailAsync(id);
            if (!success)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy người dùng." });
            }

            // Ghi log
            _logger.LogInformation("Verification email resent for user {UserId} by admin", id);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email for user {UserId}", id);
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi gửi email." });
        }
    }
} 