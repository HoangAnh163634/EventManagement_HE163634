using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EventManagement.Pages.Admin;

public class CalendarSyncsModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly EventService _eventService;
    private readonly ILogger<CalendarSyncsModel> _logger;

    public CalendarSyncsModel(
        AdminService adminService,
        EventService eventService,
        ILogger<CalendarSyncsModel> logger)
    {
        _adminService = adminService;
        _eventService = eventService;
        _logger = logger;
    }

    public List<CalendarSync> CalendarSyncs { get; set; } = new();
    public List<Event> Events { get; set; } = new();
    public (List<User> users, int totalItems) UsersData { get; set; }
    public List<User> Users => UsersData.users;
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EventId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Provider { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SyncStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsActive { get; set; }

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
            Events = await _eventService.GetEventsAsync();

            var (calendarSyncs, totalItems) = await _adminService.GetCalendarSyncsAsync(
                SearchTerm, EventId, Provider, SyncStatus, IsActive,
                SortBy, SortOrder, CurrentPage, PageSize);

            CalendarSyncs = calendarSyncs;
            TotalItems = totalItems;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading calendar syncs list");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đồng bộ lịch.";
            return RedirectToPage("/Index");
        }
    }

    public async Task<IActionResult> OnPostCreateAsync([FromBody] CreateCalendarSyncModel model)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            await _adminService.CreateCalendarSyncAsync(
                model.EventId,
                model.UserId,
                model.Provider,
                model.ExternalCalendarId);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating calendar sync");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi tạo đồng bộ." });
        }
    }

    public async Task<IActionResult> OnPostSyncNowAsync([FromBody] SyncNowModel model)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            await _adminService.SyncCalendarAsync(model.SyncId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing calendar");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi đồng bộ." });
        }
    }

    public async Task<IActionResult> OnPostToggleAsync([FromBody] ToggleCalendarSyncModel model)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            await _adminService.ToggleCalendarSyncAsync(model.SyncId, model.IsActive);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling calendar sync");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi thay đổi trạng thái." });
        }
    }

    public async Task<IActionResult> OnPostSyncAllAsync()
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }
            var allSyncs = await _adminService.GetCalendarSyncsAsync(null, null, null, null, true, "date", "desc", 1, int.MaxValue);
            int successCount = 0, failCount = 0;
            foreach (var sync in allSyncs.Item1)
            {
                try
                {
                    await _adminService.SyncCalendarAsync(sync.SyncId);
                    successCount++;
                }
                catch
                {
                    failCount++;
                }
            }
            return new JsonResult(new { success = true, message = $"Đã đồng bộ {successCount} / {allSyncs.Item1.Count} sự kiện thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all calendar events");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi đồng bộ toàn bộ." });
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
            { "provider", Provider },
            { "syncStatus", SyncStatus },
            { "isActive", IsActive?.ToString() },
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

public class CreateCalendarSyncModel
{
    public int EventId { get; set; }
    public int UserId { get; set; }
    public string Provider { get; set; } = null!;
    public string ExternalCalendarId { get; set; } = null!;
}

public class SyncNowModel
{
    public int SyncId { get; set; }
}

public class ToggleCalendarSyncModel
{
    public int SyncId { get; set; }
    public bool IsActive { get; set; }
} 