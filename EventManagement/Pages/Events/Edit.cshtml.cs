using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models.ViewModels;
using EventManagement.Services;

namespace EventManagement.Pages.Events;

public class EditModel : PageModel
{
    private readonly EventService _eventService;

    public EditModel(EventService eventService)
    {
        _eventService = eventService;
    }

    [BindProperty]
    public EventViewModel Event { get; set; } = new();

    [BindProperty]
    public bool RemoveBanner { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Check if user is logged in
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để chỉnh sửa sự kiện.";
            return RedirectToPage("/Account/Login");
        }

        // Check if user's email is verified
        var isEmailVerified = HttpContext.Session.GetString("UserEmailVerified") == "true";
        if (!isEmailVerified)
        {
            TempData["ErrorMessage"] = "Bạn cần xác thực email để chỉnh sửa sự kiện.";
            return RedirectToPage("/Index");
        }

        // Get event and check if user is organizer
        var evt = await _eventService.GetEventByIdAsync(id);
        if (evt == null)
        {
            return NotFound();
        }

        var isOrganizer = await _eventService.IsOrganizerAsync(id, userId.Value);
        if (!isOrganizer)
        {
            TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa sự kiện này.";
            return RedirectToPage("/Events/Details", new { id });
        }

        // Map event to view model
        Event = new EventViewModel
        {
            EventId = evt.EventId,
            EventName = evt.EventName,
            Description = evt.Description,
            EventTypeId = evt.EventTypeId,
            StartDate = evt.StartDate,
            EndDate = evt.EndDate,
            Location = evt.Location,
            Address = evt.Address,
            IsPublic = evt.IsPublic,
            PrivacyLevel = evt.PrivacyLevel,
            MaxAttendees = evt.MaxAttendees,
            Status = evt.Status,
            RegistrationDeadline = evt.RegistrationDeadline,
            Price = evt.Price,
            Currency = evt.Currency,
            BannerImageUrl = evt.BannerImageUrl,
            Tags = evt.Tags
        };

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

            var isOrganizer = await _eventService.IsOrganizerAsync(Event.EventId, userId.Value);
            if (!isOrganizer)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa sự kiện này.";
                return RedirectToPage("/Events/Details", new { id = Event.EventId });
            }

            if (RemoveBanner)
            {
                Event.BannerImageUrl = null;
            }

            var updatedEvent = await _eventService.UpdateEventAsync(Event);
            TempData["SuccessMessage"] = "Sự kiện đã được cập nhật thành công!";
            return RedirectToPage("/Events/Details", new { id = updatedEvent.EventId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật sự kiện. Vui lòng thử lại.");
            Event.EventTypes = await _eventService.GetEventTypesAsync();
            return Page();
        }
    }
} 