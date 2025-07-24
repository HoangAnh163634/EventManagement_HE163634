using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventManagement.Models;

namespace EventManagement.Pages.Events;

public class FeedbackListModel : PageModel
{
    private readonly EventManagementDbContext _context;
    public FeedbackListModel(EventManagementDbContext context)
    {
        _context = context;
    }

    public Event? Event { get; set; }
    public List<Feedback> Feedbacks { get; set; } = new();
    public int? Rating { get; set; }
    public bool? IsPublic { get; set; }
    public bool? WouldRecommend { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 5;
    public int TotalPages { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(int? eventId, int? rating, bool? isPublic, bool? wouldRecommend, string? search, int page = 1)
    {
        if (eventId == null)
        {
            return NotFound();
        }
        Event = await _context.Events
            .Include(e => e.Feedbacks)
                .ThenInclude(f => f.Attendee)
            .FirstOrDefaultAsync(e => e.EventId == eventId && !e.IsDeleted);
        if (Event == null)
        {
            return NotFound();
        }
        var query = Event.Feedbacks.AsQueryable();
        if (rating.HasValue)
        {
            query = query.Where(f => f.Rating == rating.Value);
        }
        if (isPublic.HasValue)
        {
            query = query.Where(f => f.IsPublic == isPublic.Value);
        }
        if (wouldRecommend.HasValue)
        {
            query = query.Where(f => f.WouldRecommend == wouldRecommend.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f => (f.Comments != null && f.Comments.ToLower().Contains(search.ToLower())) ||
                                      (f.Suggestions != null && f.Suggestions.ToLower().Contains(search.ToLower())));
        }
        query = query.OrderByDescending(f => f.SubmittedAt);
        // Phân trang
        PageNumber = page < 1 ? 1 : page;
        int total = query.Count();
        TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        Feedbacks = query.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
        // Lưu lại filter cho view
        Rating = rating;
        IsPublic = isPublic;
        WouldRecommend = wouldRecommend;
        Search = search;
        return Page();
    }
} 