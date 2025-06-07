
using EventManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventManagement.Pages
{
    public class IndexModel : PageModel
    {
        private readonly EventManagementContext _context;

        public IndexModel(EventManagementContext context)
        {
            _context = context;
        }

        public IList<Event> Events { get; set; }

        public async Task OnGetAsync()
        {
            Events = await _context.Events
                .Include(e => e.EventType)
                .Where(e => e.IsPublic && (e.Status == "Upcoming" || e.Status == "Ongoing"))
                .OrderByDescending(e => e.CreatedAt) // Latest 3 events
                .Take(3)
                .ToListAsync();
        }
    }
}
