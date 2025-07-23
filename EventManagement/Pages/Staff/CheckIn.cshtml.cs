using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using Microsoft.Extensions.Logging;

namespace EventManagement.Pages.Staff;

public class CheckInModel : PageModel
{
    private readonly EventService _eventService;
    private readonly RegistrationService _registrationService;
    private readonly ILogger<CheckInModel> _logger;

    public CheckInModel(
        EventService eventService,
        RegistrationService registrationService,
        ILogger<CheckInModel> logger)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _logger = logger;
    }

    public Event? Event { get; set; }
    public List<Registration> RecentCheckIns { get; set; } = new();
    public List<Registration> SearchResults { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? EventId { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Chỉ cho phép Staff truy cập
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Staff")
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
            return RedirectToPage("/Index");
        }

        if (EventId == null)
        {
            return Page();
        }

        Event = await _eventService.GetEventByIdAsync(EventId.Value);
        if (Event == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
            return Page();
        }

        // Lấy lịch sử check-in gần đây
        RecentCheckIns = Event.Registrations
            .Where(r => r.Status == "CheckedIn")
            .OrderByDescending(r => r.CheckInTime)
            .Take(10)
            .ToList();

        // Tìm kiếm attendee
        if (!string.IsNullOrEmpty(Search))
        {
            var searchTerm = Search.ToLower();
            SearchResults = Event.Registrations
                .Where(r =>
                    (r.Attendee.FullName.ToLower().Contains(searchTerm) ||
                    r.Attendee.Email.ToLower().Contains(searchTerm)) &&
                    !r.IsDeleted)
                .OrderBy(r => r.Attendee.FullName)
                .ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostManualAsync(int? eventId, string qrCode)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return new JsonResult(new { success = false, message = "Bạn cần đăng nhập để check-in." });
        }
        if (eventId == null)
        {
            return new JsonResult(new { success = false, message = "Thiếu thông tin sự kiện." });
        }
        try
        {
            var isValid = await _registrationService.ValidateQrCodeAsync(qrCode);
            if (!isValid)
            {
                return new JsonResult(new { success = false, message = "QR code không hợp lệ hoặc đã hết hạn." });
            }
            var registration = await _registrationService.CheckInAsync(eventId.Value, userId.Value, "QR Code", "");
            return new JsonResult(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi check-in thủ công cho staff");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi check-in. Vui lòng thử lại." });
        }
    }

    public string GetStatusClass(string status)
    {
        return status.ToLower() switch
        {
            "registered" => "info",
            "checkedin" => "success",
            "cancelled" => "danger",
            _ => "secondary"
        };
    }
} 