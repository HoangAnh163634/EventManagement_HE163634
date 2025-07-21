using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventManagement.Pages.Events;

public class FeedbackModel : PageModel
{
    private readonly EventManagementDbContext _context;
    private readonly ILogger<FeedbackModel> _logger;

    public FeedbackModel(EventManagementDbContext context, ILogger<FeedbackModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Event? Event { get; set; }
    public bool CanSubmitFeedback { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var registration = await _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.Feedback)
            .FirstOrDefaultAsync(r => r.RegistrationId == id && r.AttendeeId == userId);

        if (registration == null)
        {
            return NotFound();
        }

        if (registration.Status != "Attended")
        {
            return RedirectToPage("/Events/MyRegistrations");
        }

        if (registration.Feedback != null)
        {
            return RedirectToPage("/Events/MyRegistrations");
        }

        Event = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.EventId == registration.EventId);

        if (Event == null)
        {
            return NotFound();
        }

        // Check if user can submit feedback
        CanSubmitFeedback = registration.Status == "CheckedIn" &&
                           Event.EndDate <= DateTime.UtcNow;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, int rating, string comments, string? suggestions, bool wouldRecommend, bool isPublic)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            var registration = await _context.Registrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RegistrationId == id && r.AttendeeId == userId);

            if (registration == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đăng ký.";
                return RedirectToPage("/Events/Details", new { id });
            }

            if (registration.Status != "Attended")
            {
                TempData["ErrorMessage"] = "Bạn cần check-in trước khi đánh giá.";
                return RedirectToPage("/Events/Details", new { id });
            }

            if (registration.Event.EndDate > DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể đánh giá sau khi sự kiện kết thúc.";
                return RedirectToPage("/Events/Details", new { id });
            }

            if (registration.Feedback != null)
            {
                TempData["ErrorMessage"] = "Bạn đã đánh giá sự kiện này.";
                return RedirectToPage("/Events/Details", new { id });
            }

            var feedback = new Feedback
            {
                RegistrationId = registration.RegistrationId,
                EventId = registration.EventId,
                AttendeeId = userId,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback for event ID: {EventId}", id);
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