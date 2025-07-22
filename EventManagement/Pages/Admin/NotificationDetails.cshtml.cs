using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Threading.Tasks;

namespace EventManagement.Pages.Admin;

public class NotificationDetailsModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly ILogger<NotificationDetailsModel> _logger;

    public NotificationDetailsModel(
        AdminService adminService,
        ILogger<NotificationDetailsModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public Notification? Notification { get; set; }

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

            // Lấy thông tin thông báo
            Notification = await _adminService.GetNotificationByIdAsync(id);
            if (Notification == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông báo.";
                return RedirectToPage("/Admin/Notifications");
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading notification details for notification {NotificationId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin thông báo.";
            return RedirectToPage("/Admin/Notifications");
        }
    }
} 