using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using EventManagement.Models;
using EventManagement.Services;

namespace EventManagement.Pages.Events;

public class IndexModel : PageModel
{
    private readonly EventService _eventService;

    public IndexModel(EventService eventService)
    {
        _eventService = eventService;
    }

    public List<Event> Events { get; set; } = new();
    public SelectList? EventTypeList { get; set; }
    public SelectList? StatusList { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EventTypeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PriceRange { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "date";

    [BindProperty(SupportsGet = true)]
    public string SortOrder { get; set; } = "asc";

    public bool HasActiveFilters => 
        !string.IsNullOrEmpty(SearchTerm) ||
        EventTypeId.HasValue ||
        !string.IsNullOrEmpty(Status) ||
        !string.IsNullOrEmpty(PriceRange) ||
        StartDate.HasValue ||
        EndDate.HasValue ||
        SortBy != "date" ||
        SortOrder != "asc";

    public async Task OnGetAsync()
    {
        // Get event types for dropdown
        var eventTypes = await _eventService.GetEventTypesAsync();
        EventTypeList = new SelectList(eventTypes, "EventTypeId", "EventTypeName");

        // Get statuses for dropdown
        var statuses = new[] { "Upcoming", "Ongoing", "Completed", "Cancelled" };
        StatusList = new SelectList(statuses);

        // Parse price range
        decimal? minPrice = null;
        decimal? maxPrice = null;
        if (!string.IsNullOrEmpty(PriceRange))
        {
            switch (PriceRange)
            {
                case "free":
                    maxPrice = 0;
                    break;
                case "paid":
                    minPrice = 0;
                    break;
                case "0-100000":
                    minPrice = 0;
                    maxPrice = 100000;
                    break;
                case "100000-500000":
                    minPrice = 100000;
                    maxPrice = 500000;
                    break;
                case "500000+":
                    minPrice = 500000;
                    break;
            }
        }

        // Get filtered events
        Events = await _eventService.SearchEventsAsync(
            searchTerm: SearchTerm,
            eventTypeId: EventTypeId,
            status: Status,
            startDate: StartDate,
            endDate: EndDate,
            minPrice: minPrice,
            maxPrice: maxPrice,
            sortBy: SortBy,
            sortOrder: SortOrder);
    }

    public string GetStatusClass(string status)
    {
        return status.ToLower() switch
        {
            "upcoming" => "warning",
            "ongoing" => "success",
            "completed" => "info",
            "cancelled" => "danger",
            _ => "secondary"
        };
    }
} 