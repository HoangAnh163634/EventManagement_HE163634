using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Models.ViewModels;
using EventManagement.Services;

namespace EventManagement.Pages.Events;

public class RegisterModel : PageModel
{
    private readonly EventService _eventService;
    private readonly RegistrationService _registrationService;

    public RegisterModel(
        EventService eventService,
        RegistrationService registrationService)
    {
        _eventService = eventService;
        _registrationService = registrationService;
    }

    public Event? Event { get; set; }

    [BindProperty]
    public RegistrationViewModel Registration { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Check if user is logged in
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để đăng ký tham gia sự kiện.";
            return RedirectToPage("/Account/Login");
        }

        // Check if user's email is verified
        var isEmailVerified = HttpContext.Session.GetString("UserEmailVerified") == "true";
        if (!isEmailVerified)
        {
            TempData["ErrorMessage"] = "Bạn cần xác thực email để đăng ký tham gia sự kiện.";
            return RedirectToPage("/Index");
        }

        // Get event
        Event = await _eventService.GetEventByIdAsync(id);
        if (Event == null)
        {
            return NotFound();
        }

        // Check if user can register
        var canRegister = await _eventService.CanRegisterAsync(id);
        if (!canRegister)
        {
            TempData["ErrorMessage"] = "Không thể đăng ký tham gia sự kiện này.";
            return RedirectToPage("/Events/Details", new { id });
        }

        // Check if user has already registered
        var hasRegistered = await _eventService.HasRegisteredAsync(id, userId.Value);
        if (hasRegistered)
        {
            TempData["ErrorMessage"] = "Bạn đã đăng ký tham gia sự kiện này.";
            return RedirectToPage("/Events/Details", new { id });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            Event = await _eventService.GetEventByIdAsync(id);
            return Page();
        }

        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var registration = await _registrationService.RegisterForEventAsync(
                id,
                userId.Value,
                Registration.SpecialRequests);

            TempData["SuccessMessage"] = "Đăng ký tham gia sự kiện thành công! Vui lòng kiểm tra email để nhận thông tin chi tiết.";
            return RedirectToPage("/Events/Details", new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            Event = await _eventService.GetEventByIdAsync(id);
            return Page();
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đăng ký. Vui lòng thử lại.");
            Event = await _eventService.GetEventByIdAsync(id);
            return Page();
        }
    }
} 