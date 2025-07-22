using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EventManagement.Pages.Admin;

public class FeedbacksModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly EventService _eventService;
    private readonly ILogger<FeedbacksModel> _logger;

    public FeedbacksModel(
        AdminService adminService,
        EventService eventService,
        ILogger<FeedbacksModel> logger)
    {
        _adminService = adminService;
        _eventService = eventService;
        _logger = logger;
    }

    public List<Feedback> Feedbacks { get; set; } = new();
    public List<Event> Events { get; set; } = new();
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EventId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Rating { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsApproved { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsPublic { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "date";

    [BindProperty(SupportsGet = true)]
    public string SortOrder { get; set; } = "desc";

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToPage("/Index");
            }

            // Đảm bảo Page được binding đúng
            CurrentPage = Page;
            Events = await _eventService.GetEventsAsync();

            var (feedbacks, totalItems) = await _adminService.GetFeedbacksAsync(
                SearchTerm, EventId, Rating, IsApproved, IsPublic,
                SortBy, SortOrder, CurrentPage, PageSize);

            Feedbacks = feedbacks;
            TotalItems = totalItems;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading feedbacks list");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách feedback.";
            return RedirectToPage("/Index");
        }
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync([FromBody] UpdateFeedbackStatusModel model)
    {
        try
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return new JsonResult(new { success = false, message = "Không có quyền truy cập." });
            }

            await _adminService.UpdateFeedbackStatusAsync(model.FeedbackId, model.IsApproved, model.IsPublic);
            
            _logger.LogInformation(
                "Feedback {FeedbackId} status updated by admin. IsApproved: {IsApproved}, IsPublic: {IsPublic}",
                model.FeedbackId,
                model.IsApproved,
                model.IsPublic);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feedback status");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái." });
        }
    }

    public string GetSortUrl(string column)
    {
        var newOrder = SortBy == column && SortOrder == "asc" ? "desc" : "asc";
        var queryParams = new Dictionary<string, string?>
        {
            { "sortBy", column },
            { "sortOrder", newOrder },
            { "searchTerm", SearchTerm },
            { "eventId", EventId?.ToString() },
            { "rating", Rating?.ToString() },
            { "isApproved", IsApproved?.ToString() },
            { "isPublic", IsPublic?.ToString() },
            { "page", "1" }
        };

        return $"{Request.Path}?{string.Join("&", queryParams.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"))}";
    }

    public string GetPageUrl(int pageNumber)
    {
        var queryParams = Request.QueryString.Value ?? "";
        if (queryParams.Contains("page="))
        {
            queryParams = System.Text.RegularExpressions.Regex.Replace(queryParams, @"page=\d+", $"page={pageNumber}");
        }
        else
        {
            queryParams += (queryParams.Contains("?") ? "&" : "?") + $"page={pageNumber}";
        }
        return Request.Path + queryParams;
    }
}

public class UpdateFeedbackStatusModel
{
    public int FeedbackId { get; set; }
    public bool IsApproved { get; set; }
    public bool IsPublic { get; set; }
} 