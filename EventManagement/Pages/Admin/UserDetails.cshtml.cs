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
    private readonly ILogger<UserDetailsModel> _logger;

    public UserDetailsModel(AdminService adminService, ILogger<UserDetailsModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public User? User { get; set; }
    public List<Role> Roles { get; set; } = new();
    public List<int> SelectedRoles { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        User = await _adminService.GetUserByIdAsync(id.Value);
        if (User == null)
        {
            return NotFound();
        }

        Roles = await _adminService.GetAllRolesAsync();
        SelectedRoles = User.UserRoleUsers?.Select(ur => ur.RoleId).ToList() ?? new List<int>();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, bool isActive, List<int> selectedRoles)
    {
        try
        {
            await _adminService.UpdateUserAsync(id, isActive, selectedRoles.ToArray());
            TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công.";
            return RedirectToPage("/Admin/Users");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thông tin người dùng.";
            return RedirectToPage("/Admin/UserDetails", new { id });
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
            var user = await _adminService.GetUserByIdAsync(id);
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