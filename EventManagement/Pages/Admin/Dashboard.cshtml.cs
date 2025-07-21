using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models.ViewModels;
using EventManagement.Services;
using System.Threading.Tasks;

namespace EventManagement.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(AdminService adminService, ILogger<DashboardModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public AdminDashboardViewModel Dashboard { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Kiểm tra phân quyền
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để truy cập trang này.";
                return RedirectToPage("/Account/Login");
            }

            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToPage("/Index");
            }

            // Lấy dữ liệu dashboard
            Dashboard = await _adminService.GetDashboardAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu. Vui lòng thử lại sau.";
            return RedirectToPage("/Index");
        }
    }
} 