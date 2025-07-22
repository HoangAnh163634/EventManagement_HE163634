using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EventManagement.Pages.Admin;

public class RegistrationsModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly EventService _eventService;
    private readonly ILogger<RegistrationsModel> _logger;

    public RegistrationsModel(
        AdminService adminService,
        EventService eventService,
        ILogger<RegistrationsModel> logger)
    {
        _adminService = adminService;
        _eventService = eventService;
        _logger = logger;
    }

    public List<Registration> Registrations { get; set; } = new();
    public List<Event> Events { get; set; } = new();
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EventId { get; set; }

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
            Events = await _eventService.GetEventsAsync();

            var (registrations, totalItems) = await _adminService.GetRegistrationsAsync(
                SearchTerm, EventId, Status, StartDate, EndDate,
                SortBy, SortOrder, CurrentPage, PageSize);

            Registrations = registrations;
            TotalItems = totalItems;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading registrations list");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đăng ký.";
            return RedirectToPage("/Index");
        }
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync([FromBody] UpdateRegistrationStatusModel model)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            await _adminService.UpdateRegistrationStatusAsync(model.RegistrationId, model.Status);
            
            _logger.LogInformation(
                "Registration {RegistrationId} status updated to {Status} by admin",
                model.RegistrationId,
                model.Status);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating registration status");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái." });
        }
    }

    public async Task<IActionResult> OnPostCheckInAsync([FromBody] CheckInModel model)
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

            await _adminService.CheckInRegistrationAsync(
                model.RegistrationId,
                userId.Value,
                model.CheckInMethod,
                model.CheckInLocation);
            
            _logger.LogInformation(
                "Registration {RegistrationId} checked in by admin {UserId}. Method: {Method}, Location: {Location}",
                model.RegistrationId,
                userId.Value,
                model.CheckInMethod,
                model.CheckInLocation);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in registration");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi check-in." });
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

public class UpdateRegistrationStatusModel
{
    public int RegistrationId { get; set; }
    public string Status { get; set; } = null!;
}

public class CheckInModel
{
    public int RegistrationId { get; set; }
    public string CheckInMethod { get; set; } = null!;
    public string CheckInLocation { get; set; } = null!;
} 