using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;

namespace EventManagement.Pages.Notifications;

public class IndexModel : PageModel
{
    private readonly NotificationService _notificationService;

    public IndexModel(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public List<Notification> Notifications { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem thông báo.";
            return RedirectToPage("/Account/Login");
        }

        Notifications = await _notificationService.GetUserNotificationsAsync(userId.Value);

        // Apply filters
        if (!string.IsNullOrEmpty(Type))
        {
            Notifications = Notifications.Where(n => n.NotificationType == Type).ToList();
        }

        if (!string.IsNullOrEmpty(Status))
        {
            var isRead = Status == "read";
            Notifications = Notifications.Where(n => n.IsRead == isRead).ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(int notificationId)
    {
        await _notificationService.MarkAsReadAsync(notificationId);
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        await _notificationService.MarkAllAsReadAsync(userId.Value);
        TempData["SuccessMessage"] = "Đã đánh dấu tất cả thông báo là đã đọc.";
        return RedirectToPage();
    }

    public string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;

        if (span.TotalDays > 30)
            return dateTime.ToString("dd/MM/yyyy HH:mm");
        if (span.TotalDays > 1)
            return $"{(int)span.TotalDays} ngày trước";
        if (span.TotalHours > 1)
            return $"{(int)span.TotalHours} giờ trước";
        if (span.TotalMinutes > 1)
            return $"{(int)span.TotalMinutes} phút trước";
        return "Vừa xong";
    }
} 