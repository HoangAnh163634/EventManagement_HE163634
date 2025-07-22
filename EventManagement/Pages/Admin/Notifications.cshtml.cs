using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EventManagement.Pages.Admin;

public class NotificationsModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly ILogger<NotificationsModel> _logger;

    public NotificationsModel(AdminService adminService, ILogger<NotificationsModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public (List<Event> events, int totalItems) EventsData { get; set; }
    public List<Event> Events => EventsData.events;
    public (List<User> users, int totalItems) UsersData { get; set; }
    public List<User> Users => UsersData.users;

    public List<Notification> Notifications { get; set; } = new();
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EventId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? NotificationType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Priority { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "date";

    [BindProperty(SupportsGet = true)]
    public string SortOrder { get; set; } = "desc";

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToPage("/Index");
            }

            // Đảm bảo Page được binding đúng
            CurrentPage = Page;
            EventsData = await _adminService.GetEventsAsync(searchTerm: null, eventTypeId: null, status: null, startDate: null, endDate: null, sortBy: "date", sortOrder: "desc", page: 1, pageSize: 1000);
            UsersData = await _adminService.GetUsersAsync(searchTerm: null, role: null, isActive: null, startDate: null, endDate: null, sortBy: "date", sortOrder: "desc", page: 1, pageSize: 1000);

            var (notifications, totalItems) = await _adminService.GetNotificationsAsync(
                SearchTerm, EventId, NotificationType, Priority, Status,
                SortBy, SortOrder, CurrentPage, PageSize);

            Notifications = notifications;
            TotalItems = totalItems;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading notifications list");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thông báo.";
            return RedirectToPage("/Index");
        }
    }

    public async Task<IActionResult> OnPostCreateAsync([FromBody] CreateNotificationModel model)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return new JsonResult(new { success = false, message = "Không xác định được người dùng." });
            }

            await _adminService.CreateNotificationAsync(
                model.NotificationType,
                model.Priority,
                model.EventId,
                model.UserId,
                model.Title,
                model.Subject,
                model.Body,
                model.Link,
                userId.Value);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi tạo thông báo." });
        }
    }

    public async Task<IActionResult> OnPostResendAsync([FromBody] ResendNotificationModel model)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            await _adminService.ResendNotificationAsync(model.NotificationId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending notification");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi gửi lại thông báo." });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteNotificationModel model)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            await _adminService.DeleteNotificationAsync(model.NotificationId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi xóa thông báo." });
        }
    }

    public string GetSortUrl(string column)
    {
        var newOrder = SortBy == column && SortOrder == "asc" ? "desc" : "asc";
        var queryParams = new Dictionary<string, string?>
        {
            { "sortBy", column },
            { "sortOrder", newOrder },
            { "searchTerm", SearchTerm },
            { "eventId", EventId?.ToString() },
            { "notificationType", NotificationType },
            { "priority", Priority },
            { "status", Status },
            { "page", "1" }
        };

        return $"{Request.Path}?{string.Join("&", queryParams.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"))}";
    }

    public string GetPageUrl(int pageNumber)
    {
        var queryParams = Request.QueryString.Value ?? "";
        if (queryParams.Contains("page="))
        {
            queryParams = System.Text.RegularExpressions.Regex.Replace(queryParams, @"page=\d+", $"page={pageNumber}");
        }
        else
        {
            queryParams += (queryParams.Contains("?") ? "&" : "?") + $"page={pageNumber}";
        }
        return Request.Path + queryParams;
    }
}

public class CreateNotificationModel
{
    public string NotificationType { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public int? EventId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? Link { get; set; }
}

public class ResendNotificationModel
{
    public int NotificationId { get; set; }
}

public class DeleteNotificationModel
{
    public int NotificationId { get; set; }
} 