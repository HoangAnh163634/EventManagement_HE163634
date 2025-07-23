using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventManagement.Pages.Account;

public class NotificationsModel : PageModel
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<NotificationsModel> _logger;

    public NotificationsModel(NotificationService notificationService, ILogger<NotificationsModel> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public List<Notification> Notifications { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)System.Math.Ceiling(TotalItems / (double)PageSize);
    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userId == null || userRole != "User")
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
            return RedirectToPage("/Index");
        }
        var allNoti = await _notificationService.GetUserNotificationsAsync(userId.Value);
        TotalItems = allNoti.Count;
        CurrentPage = Page;
        Notifications = allNoti
            .OrderByDescending(n => n.SentAt)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
        return Page();
    }
} 