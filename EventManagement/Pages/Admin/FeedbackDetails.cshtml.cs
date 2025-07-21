using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Threading.Tasks;

namespace EventManagement.Pages.Admin;

public class FeedbackDetailsModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly ILogger<FeedbackDetailsModel> _logger;

    public FeedbackDetailsModel(
        AdminService adminService,
        ILogger<FeedbackDetailsModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public Feedback? Feedback { get; set; }

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

            // Lấy thông tin feedback
            Feedback = await _adminService.GetFeedbackByIdAsync(id);
            if (Feedback == null)
            {
                return Page();
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading feedback details for feedback {FeedbackId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin feedback.";
            return RedirectToPage("/Admin/Feedbacks");
        }
    }
} 