using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;

namespace EventManagement.Pages.Events;

public class MyRegistrationsModel : PageModel
{
    private readonly RegistrationService _registrationService;

    public MyRegistrationsModel(RegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    public List<Registration> Registrations { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem danh sách đăng ký.";
            return RedirectToPage("/Account/Login");
        }

        Registrations = await _registrationService.GetUserRegistrationsAsync(userId.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int registrationId, string? reason)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            await _registrationService.CancelRegistrationAsync(registrationId, userId.Value, reason);
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

        return RedirectToPage();
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