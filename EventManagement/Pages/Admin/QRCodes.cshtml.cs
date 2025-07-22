using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EventManagement.Pages.Admin;

public class QRCodesModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly EventService _eventService;
    private readonly ILogger<QRCodesModel> _logger;

    public QRCodesModel(
        AdminService adminService,
        EventService eventService,
        ILogger<QRCodesModel> logger)
    {
        _adminService = adminService;
        _eventService = eventService;
        _logger = logger;
    }

    public List<Qrcode> QRCodes { get; set; } = new();
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
    public bool? IsActive { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsUsed { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? RegistrationStatus { get; set; }

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

            var (qrCodes, totalItems) = await _adminService.GetQRCodesAsync(
                SearchTerm, EventId, IsActive, IsUsed, RegistrationStatus,
                SortBy, SortOrder, CurrentPage, PageSize);

            QRCodes = qrCodes;
            TotalItems = totalItems;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading QR codes list");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách QR Code.";
            return RedirectToPage("/Index");
        }
    }

    public async Task<IActionResult> OnGetRegistrationsAsync(int eventId)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            var registrations = await _adminService.GetRegistrationsForQRCodeAsync(eventId);
            var result = registrations.Select(r => new
            {
                registrationId = r.RegistrationId,
                attendeeName = r.Attendee.FullName,
                attendeeEmail = r.Attendee.Email
            });

            return new JsonResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registrations for QR code");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi lấy danh sách đăng ký." });
        }
    }

    public async Task<IActionResult> OnPostGenerateAsync([FromBody] GenerateQRCodeModel model)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            await _adminService.GenerateQRCodeAsync(
                model.EventId,
                model.RegistrationId,
                model.ExpiresAt);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi tạo QR Code." });
        }
    }

    public async Task<IActionResult> OnPostScanAsync([FromBody] ScanQRCodeModel model)
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

            await _adminService.ScanQRCodeAsync(
                model.QRCodeValue,
                userId.Value,
                model.CheckInMethod,
                model.CheckInLocation);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning QR code");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi quét QR Code." });
        }
    }

    public async Task<IActionResult> OnPostDeactivateAsync([FromBody] DeactivateQRCodeModel model)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            await _adminService.DeactivateQRCodeAsync(model.QRCodeId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating QR code");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi vô hiệu hóa QR Code." });
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
            { "isActive", IsActive?.ToString() },
            { "isUsed", IsUsed?.ToString() },
            { "registrationStatus", RegistrationStatus },
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

public class GenerateQRCodeModel
{
    public int EventId { get; set; }
    public int RegistrationId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ScanQRCodeModel
{
    public string QRCodeValue { get; set; } = null!;
    public string CheckInMethod { get; set; } = null!;
    public string CheckInLocation { get; set; } = null!;
}

public class DeactivateQRCodeModel
{
    public int QRCodeId { get; set; }
} 