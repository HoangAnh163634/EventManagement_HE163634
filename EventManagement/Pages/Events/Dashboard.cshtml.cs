using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;

namespace EventManagement.Pages.Events;

public class DashboardModel : PageModel
{
    private readonly EventService _eventService;

    public DashboardModel(EventService eventService)
    {
        _eventService = eventService;
    }

    public Event? Event { get; set; }
    public List<string> RegistrationDates { get; set; } = new();
    public List<int> RegistrationCounts { get; set; } = new();
    public List<string> CheckInHours { get; set; } = new();
    public List<int> CheckInCounts { get; set; } = new();
    public List<int> RatingCounts { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Check if user is logged in and is organizer
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem dashboard.";
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
            TempData["ErrorMessage"] = "Bạn không có quyền xem dashboard của sự kiện này.";
            return RedirectToPage("/Events/Details", new { id });
        }

        // Calculate registration stats by date
        var registrations = Event.Registrations
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.RegistrationDate.Date)
            .GroupBy(r => r.RegistrationDate.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var startDate = registrations.Keys.Min();
        var endDate = registrations.Keys.Max();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            RegistrationDates.Add(currentDate.ToString("dd/MM"));
            RegistrationCounts.Add(registrations.ContainsKey(currentDate) ? registrations[currentDate] : 0);
            currentDate = currentDate.AddDays(1);
        }

        // Calculate check-in stats by hour
        var checkIns = Event.Registrations
            .Where(r => r.Status == "CheckedIn" && r.CheckInTime.HasValue)
            .OrderBy(r => r.CheckInTime!.Value.Hour)
            .GroupBy(r => r.CheckInTime!.Value.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        for (int hour = 0; hour < 24; hour++)
        {
            CheckInHours.Add($"{hour:00}:00");
            CheckInCounts.Add(checkIns.ContainsKey(hour) ? checkIns[hour] : 0);
        }

        // Calculate rating distribution
        RatingCounts = new List<int>();
        for (int rating = 5; rating >= 1; rating--)
        {
            var count = Event.Feedbacks.Count(f => f.Rating == rating);
            RatingCounts.Add(count);
        }

        return Page();
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