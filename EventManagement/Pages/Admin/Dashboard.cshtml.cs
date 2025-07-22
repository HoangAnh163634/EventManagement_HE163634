using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models.ViewModels;
using EventManagement.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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

    // Biến trung gian cho biểu đồ
    public List<string> UserGrowthLabels { get; set; } = new();
    public List<int> UserGrowthValues { get; set; } = new();
    public List<string> EventGrowthLabels { get; set; } = new();
    public List<int> EventGrowthValues { get; set; } = new();
    public List<string> RegistrationGrowthLabels { get; set; } = new();
    public List<int> RegistrationGrowthValues { get; set; } = new();
    public List<string> TopEventsLabels { get; set; } = new();
    public List<int> TopEventsValues { get; set; } = new();

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

            // Gán dữ liệu cho các biến trung gian
            UserGrowthLabels = Dashboard.UserGrowth.Select(x => x.Date.ToString("dd/MM")).ToList();
            UserGrowthValues = Dashboard.UserGrowth.Select(x => x.Value).ToList();
            EventGrowthLabels = Dashboard.EventGrowth.Select(x => x.Date.ToString("MM/dd")).ToList();
            EventGrowthValues = Dashboard.EventGrowth.Select(x => x.Value).ToList();
            RegistrationGrowthLabels = Dashboard.RegistrationGrowth.Select(x => x.Date.ToString("MM/dd")).ToList();
            RegistrationGrowthValues = Dashboard.RegistrationGrowth.Select(x => x.Value).ToList();
            TopEventsLabels = Dashboard.TopEvents.Select(x => x.EventName).ToList();
            TopEventsValues = Dashboard.TopEvents.Select(x => x.RegistrationCount).ToList();

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