using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;

namespace EventManagement.Pages.Events;

public class CheckInModel : PageModel
{
    private readonly EventService _eventService;
    private readonly RegistrationService _registrationService;

    public CheckInModel(
        EventService eventService,
        RegistrationService registrationService)
    {
        _eventService = eventService;
        _registrationService = registrationService;
    }

    public Event? Event { get; set; }
    public List<Registration> RecentCheckIns { get; set; } = new();
    public List<Registration> SearchResults { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Check if user is logged in and is organizer
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để check-in.";
            return RedirectToPage("/Account/Login");
        }

        Event = await _eventService.GetEventByIdAsync(id);
        if (Event == null)
        {
            return Page();
        }

        var isOrganizer = await _eventService.IsOrganizerAsync(id, userId.Value);
        if (!isOrganizer)
        {
            TempData["ErrorMessage"] = "Bạn không có quyền check-in cho sự kiện này.";
            return RedirectToPage("/Events/Details", new { id });
        }

        // Get recent check-ins
        RecentCheckIns = Event.Registrations
            .Where(r => r.Status == "CheckedIn")
            .OrderByDescending(r => r.CheckInTime)
            .Take(10)
            .ToList();

        // Search attendees
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

    public async Task<IActionResult> OnPostManualAsync(int id, string qrCode)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return new JsonResult(new { success = false, message = "Bạn cần đăng nhập để check-in." });
        }

        try
        {
            var isValid = await _registrationService.ValidateQrCodeAsync(qrCode);
            if (!isValid)
            {
                return new JsonResult(new { success = false, message = "QR code không hợp lệ hoặc đã hết hạn." });
            }

            var registration = await _registrationService.CheckInAsync(id, userId.Value, "QR Code", "");
            return new JsonResult(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
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