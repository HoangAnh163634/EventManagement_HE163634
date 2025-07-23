using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace EventManagement.Pages.Staff;

public class DashboardModel : PageModel
{
    private readonly EventManagementDbContext _context;
    private readonly RegistrationService _registrationService;
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(EventManagementDbContext context, RegistrationService registrationService, ILogger<DashboardModel> logger)
    {
        _context = context;
        _registrationService = registrationService;
        _logger = logger;
    }

    public List<Event> Events { get; set; } = new();
    public List<Registration> Registrations { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int TotalRegistrations => Registrations.Count;
    public int TotalCheckIns => Registrations.Count(r => r.Status == "CheckedIn");
    public int TotalFeedbacks { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để truy cập trang này.";
            return RedirectToPage("/Account/Login");
        }
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Staff")
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
            return RedirectToPage("/Index");
        }
        // Lấy tất cả sự kiện (hoặc sau này: chỉ sự kiện được phân công)
        Events = await _context.Events.Where(e => !e.IsDeleted).Include(e => e.Registrations).ToListAsync();
        Registrations = await _context.Registrations.Include(r => r.Event).Where(r => !r.IsDeleted).ToListAsync();
        TotalFeedbacks = await _context.Feedbacks.CountAsync();
        return Page();
    }
} 