using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventManagement.Models;


namespace EventManagement.Pages;

public class IndexModel : PageModel
{
    private readonly EventManagementDbContext _context;

    public IndexModel(EventManagementDbContext context)
    {
        _context = context;
    }

    public IList<Event> Events { get; set; } = new List<Event>();

    public async Task OnGetAsync()
    {
        Events = await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Where(e => e.IsPublic && !e.IsDeleted && 
                       (e.Status == "Upcoming" || e.Status == "Ongoing") &&
                       e.StartDate > DateTime.Now)
            .OrderByDescending(e => e.CreatedAt)
            .Take(3)
            .ToListAsync();
    }
}
