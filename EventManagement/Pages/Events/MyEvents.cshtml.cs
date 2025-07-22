using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventManagement.Models;
using EventManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace EventManagement.Pages.Events;

public class MyEventsModel : PageModel
{
    private readonly EventManagementDbContext _context;

    public MyEventsModel(EventManagementDbContext context)
    {
        _context = context;
    }

    public List<Event> MyEvents { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem sự kiện của mình.";
            return RedirectToPage("/Account/Login");
        }
        MyEvents = await _context.Events
            .Include(e => e.Registrations)
            .Where(e => e.OrganizerId == userId && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        return Page();
    }
} 