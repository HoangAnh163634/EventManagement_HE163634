using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Threading.Tasks;

namespace EventManagement.Pages.Admin;

public class RegistrationDetailsModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly ILogger<RegistrationDetailsModel> _logger;

    public RegistrationDetailsModel(
        AdminService adminService,
        ILogger<RegistrationDetailsModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public Registration? Registration { get; set; }

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

            // Lấy thông tin đăng ký
            Registration = await _adminService.GetRegistrationByIdAsync(id);
            if (Registration == null)
            {
                return Page();
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading registration details for registration {RegistrationId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đăng ký.";
            return RedirectToPage("/Admin/Registrations");
        }
    }
} 