using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;

namespace EventManagement.Pages.Events;

public class TicketModel : PageModel
{
    private readonly RegistrationService _registrationService;
    private readonly NotificationService _notificationService;

    public TicketModel(
        RegistrationService registrationService,
        NotificationService notificationService)
    {
        _registrationService = registrationService;
        _notificationService = notificationService;
    }

    public Registration? Registration { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem vé.";
            return RedirectToPage("/Account/Login");
        }

        Registration = await _registrationService.GetRegistrationByIdAsync(id);
        if (Registration == null)
        {
            return Page();
        }

        if (Registration.AttendeeId != userId)
        {
            TempData["ErrorMessage"] = "Bạn không có quyền xem vé này.";
            return RedirectToPage("/Events/MyRegistrations");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, string? reason)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            var registration = await _registrationService.CancelRegistrationAsync(id, userId.Value, reason);
            await _notificationService.NotifyCancelRegistrationAsync(registration);
            TempData["SuccessMessage"] = "Hủy đăng ký thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy đăng ký. Vui lòng thử lại.";
        }

        return RedirectToPage("/Events/MyRegistrations");
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