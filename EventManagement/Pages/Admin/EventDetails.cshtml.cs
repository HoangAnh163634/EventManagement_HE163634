using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Threading.Tasks;

namespace EventManagement.Pages.Admin;

public class EventDetailsModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly ILogger<EventDetailsModel> _logger;

    public EventDetailsModel(
        AdminService adminService,
        ILogger<EventDetailsModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public Event? Event { get; set; }

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

            // Lấy thông tin sự kiện
            Event = await _adminService.GetEventByIdAsync(id);
            if (Event == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
                return RedirectToPage("/Admin/Events");
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading event details for event {EventId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin sự kiện.";
            return RedirectToPage("/Admin/Events");
        }
    }
} 