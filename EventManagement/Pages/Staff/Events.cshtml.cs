using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventManagement.Pages.Staff;

public class EventsModel : PageModel
{
    private readonly EventService _eventService;
    private readonly ILogger<EventsModel> _logger;

    public EventsModel(EventService eventService, ILogger<EventsModel> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    public List<Event> Events { get; set; } = new();
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
        var allEvents = await _eventService.GetEventsAsync();
        TotalItems = allEvents.Count;
        CurrentPage = Page;
        Events = allEvents
            .OrderByDescending(e => e.StartDate)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
        return Page();
    }
} 