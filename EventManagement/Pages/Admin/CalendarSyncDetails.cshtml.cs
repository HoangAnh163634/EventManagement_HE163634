using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Threading.Tasks;

namespace EventManagement.Pages.Admin;

public class CalendarSyncDetailsModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly ILogger<CalendarSyncDetailsModel> _logger;

    public CalendarSyncDetailsModel(
        AdminService adminService,
        ILogger<CalendarSyncDetailsModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public CalendarSync? CalendarSync { get; set; }

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

            // Lấy thông tin đồng bộ
            CalendarSync = await _adminService.GetCalendarSyncByIdAsync(id);
            if (CalendarSync == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đồng bộ lịch.";
                return RedirectToPage("/Admin/CalendarSyncs");
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading calendar sync details for sync {SyncId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đồng bộ.";
            return RedirectToPage("/Admin/CalendarSyncs");
        }
    }
} 