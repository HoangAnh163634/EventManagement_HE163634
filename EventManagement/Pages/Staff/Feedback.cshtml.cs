using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventManagement.Pages.Staff;

public class FeedbackModel : PageModel
{
    private readonly EventService _eventService;
    private readonly ILogger<FeedbackModel> _logger;

    public FeedbackModel(EventService eventService, ILogger<FeedbackModel> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    public List<Event> Events { get; set; } = new();
    public List<Feedback> Feedbacks { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Staff")
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
            return RedirectToPage("/Index");
        }
        Events = await _eventService.GetEventsAsync();
        var allFeedbacks = Events.SelectMany(e => e.Feedbacks).OrderByDescending(f => f.SubmittedAt).ToList();
        TotalItems = allFeedbacks.Count;
        CurrentPage = Page;
        Feedbacks = allFeedbacks
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
        return Page();
    }
} 