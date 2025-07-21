using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventManagement.Models;
using Microsoft.AspNetCore.Http;

namespace EventManagement.Pages.Events;

public class MyFeedbackModel : PageModel
{
    private readonly EventManagementDbContext _context;
    private readonly ILogger<MyFeedbackModel> _logger;

    public MyFeedbackModel(EventManagementDbContext context, ILogger<MyFeedbackModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public List<Feedback> Feedbacks { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/Account/Login");
        }

        Feedbacks = await _context.Feedbacks
            .Include(f => f.Event)
            .Where(f => f.AttendeeId == userId.Value)
            .OrderByDescending(f => f.SubmittedAt)
            .ToListAsync();

        return Page();
    }
} 