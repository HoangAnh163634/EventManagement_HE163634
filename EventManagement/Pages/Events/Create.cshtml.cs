using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models.ViewModels;
using EventManagement.Services;

namespace EventManagement.Pages.Events;

public class CreateModel : PageModel
{
    private readonly EventService _eventService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(EventService eventService, ILogger<CreateModel> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    [BindProperty]
    public EventViewModel Event { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        // Check if user is logged in
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để tạo sự kiện.";
            return RedirectToPage("/Account/Login");
        }

        // Check if user's email is verified
        var isEmailVerified = HttpContext.Session.GetString("UserEmailVerified") == "true";
        if (!isEmailVerified)
        {
            TempData["ErrorMessage"] = "Bạn cần xác thực email để tạo sự kiện.";
            return RedirectToPage("/Index");
        }

        // Check if user is organizer or admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Organizer" && userRole != "Admin")
        {
            TempData["ErrorMessage"] = "Bạn không có quyền tạo sự kiện.";
            return RedirectToPage("/Index");
        }

        // Load event types for dropdown
        Event.EventTypes = await _eventService.GetEventTypesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Event.EventTypes = await _eventService.GetEventTypesAsync();
            return Page();
        }

        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var newEvent = await _eventService.CreateEventAsync(Event, userId.Value);
            TempData["SuccessMessage"] = "Sự kiện đã được tạo thành công!";
            return RedirectToPage("/Events/Details", new { id = newEvent.EventId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            TempData["ErrorMessage"] = $"Có lỗi xảy ra khi tạo sự kiện: {ex.Message}";
            Event.EventTypes = await _eventService.GetEventTypesAsync();
            return Page();
        }
    }
} 