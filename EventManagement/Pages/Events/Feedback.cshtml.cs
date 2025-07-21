using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;

namespace EventManagement.Pages.Events;

public class FeedbackModel : PageModel
{
    private readonly EventService _eventService;
    private readonly RegistrationService _registrationService;

    public FeedbackModel(
        EventService eventService,
        RegistrationService registrationService)
    {
        _eventService = eventService;
        _registrationService = registrationService;
    }

    public Event? Event { get; set; }
    public bool CanSubmitFeedback { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem đánh giá.";
            return RedirectToPage("/Account/Login");
        }

        Event = await _eventService.GetEventByIdAsync(id);
        if (Event == null)
        {
            return Page();
        }

        // Check if user can submit feedback
        var registration = Event.Registrations
            .FirstOrDefault(r => r.AttendeeId == userId && !r.IsDeleted);

        CanSubmitFeedback = registration != null &&
                           registration.Status == "CheckedIn" &&
                           !registration.Feedback.Any() &&
                           Event.EndDate <= DateTime.UtcNow;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, int rating, string comments, string? suggestions, bool wouldRecommend, bool isPublic)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            var registration = await _registrationService.GetRegistrationByIdAsync(id);
            if (registration == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đăng ký.";
                return RedirectToPage("/Events/Details", new { id });
            }

            if (registration.AttendeeId != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền đánh giá cho đăng ký này.";
                return RedirectToPage("/Events/Details", new { id });
            }

            if (registration.Status != "CheckedIn")
            {
                TempData["ErrorMessage"] = "Bạn cần check-in trước khi đánh giá.";
                return RedirectToPage("/Events/Details", new { id });
            }

            if (registration.Event.EndDate > DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể đánh giá sau khi sự kiện kết thúc.";
                return RedirectToPage("/Events/Details", new { id });
            }

            if (registration.Feedback.Any())
            {
                TempData["ErrorMessage"] = "Bạn đã đánh giá sự kiện này.";
                return RedirectToPage("/Events/Details", new { id });
            }

            var feedback = new Feedback
            {
                RegistrationId = registration.RegistrationId,
                EventId = registration.EventId,
                AttendeeId = userId.Value,
                Rating = rating,
                Comments = comments,
                Suggestions = suggestions,
                WouldRecommend = wouldRecommend,
                IsPublic = isPublic,
                IsApproved = true,
                SubmittedAt = DateTime.UtcNow
            };

            registration.Event.TotalFeedbacks++;
            registration.Event.AverageRating = (registration.Event.AverageRating * (registration.Event.TotalFeedbacks - 1) + rating) / registration.Event.TotalFeedbacks;

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sự kiện!";
            return RedirectToPage("/Events/Details", new { id = registration.EventId });
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi đánh giá. Vui lòng thử lại.";
            return RedirectToPage("/Events/Details", new { id });
        }
    }

    public string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;

        if (span.TotalDays > 30)
            return dateTime.ToString("dd/MM/yyyy HH:mm");
        if (span.TotalDays > 1)
            return $"{(int)span.TotalDays} ngày trước";
        if (span.TotalHours > 1)
            return $"{(int)span.TotalHours} giờ trước";
        if (span.TotalMinutes > 1)
            return $"{(int)span.TotalMinutes} phút trước";
        return "Vừa xong";
    }
} 