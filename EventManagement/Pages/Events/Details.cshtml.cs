using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using EventManagement.Data;
using Microsoft.Extensions.Logging;

namespace EventManagement.Pages.Events;

public class DetailsModel : PageModel
{
    private readonly EventService _eventService;
    private readonly EventManagementDbContext _context;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        EventService eventService,
        EventManagementDbContext context,
        ILogger<DetailsModel> logger)
    {
        _eventService = eventService;
        _context = context;
        _logger = logger;
    }

    public Event? Event { get; set; }
    public bool IsOrganizer { get; set; }
    public bool CanRegister { get; set; }
    public bool IsEmailVerified { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Event = await _eventService.GetEventByIdAsync(id);
        if (Event == null)
        {
            return Page();
        }

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId != null)
        {
            IsOrganizer = await _eventService.IsOrganizerAsync(id, userId.Value);
            CanRegister = await _eventService.CanRegisterAsync(id);
            IsEmailVerified = HttpContext.Session.GetString("UserEmailVerified") == "true";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var isOrganizer = await _eventService.IsOrganizerAsync(id, userId.Value);
        if (!isOrganizer)
        {
            TempData["ErrorMessage"] = "Bạn không có quyền xóa sự kiện này.";
            return RedirectToPage("/Events/Details", new { id });
        }

        var success = await _eventService.DeleteEventAsync(id);
        if (success)
        {
            TempData["SuccessMessage"] = "Sự kiện đã được xóa thành công.";
            return RedirectToPage("/Events");
        }

        TempData["ErrorMessage"] = "Không thể xóa sự kiện. Vui lòng thử lại.";
        return RedirectToPage("/Events/Details", new { id });
    }

    public async Task<IActionResult> OnPostLogShareAsync(int id, string platform)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var share = new SocialShare
            {
                EventId = id,
                UserId = userId,
                Platform = platform,
                SharedUrl = Request.Headers["Referer"].ToString(),
                ShareStatus = "Success",
                SharedAt = DateTime.UtcNow,
                Ipaddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            _context.SocialShares.Add(share);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging social share for event {EventId}", id);
            return new JsonResult(new { success = false, error = "Có lỗi xảy ra khi lưu thông tin chia sẻ." });
        }
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