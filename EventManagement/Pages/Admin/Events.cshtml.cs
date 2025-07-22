using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EventManagement.Pages.Admin;

public class EventsModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly EventService _eventService;
    private readonly ILogger<EventsModel> _logger;

    public EventsModel(
        AdminService adminService,
        EventService eventService,
        ILogger<EventsModel> logger)
    {
        _adminService = adminService;
        _eventService = eventService;
        _logger = logger;
    }

    public List<Event> Events { get; set; } = new();
    public List<EventType> EventTypes { get; set; } = new();
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EventTypeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

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
            EventTypes = await _eventService.GetEventTypesAsync();
            
            var (events, totalItems) = await _adminService.GetEventsAsync(
                SearchTerm, EventTypeId, Status, StartDate, EndDate,
                SortBy, SortOrder, CurrentPage, PageSize);

            Events = events;
            TotalItems = totalItems;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading events list");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách sự kiện.";
            return RedirectToPage("/Index");
        }
    }

    public async Task<IActionResult> OnPostCancelEventAsync([FromQuery] int eventId)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            await _adminService.UpdateEventStatusAsync(eventId, isCancelled: true);
            
            _logger.LogInformation("Event {EventId} cancelled by admin", eventId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling event {EventId}", eventId);
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi hủy sự kiện." });
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
            { "eventTypeId", EventTypeId?.ToString() },
            { "status", Status },
            { "startDate", StartDate?.ToString("yyyy-MM-dd") },
            { "endDate", EndDate?.ToString("yyyy-MM-dd") },
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