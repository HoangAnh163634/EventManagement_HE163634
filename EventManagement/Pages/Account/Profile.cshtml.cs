using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventManagement.Models;
using Microsoft.AspNetCore.Http;
using EventManagement.Services;
using Microsoft.Extensions.Configuration;

namespace EventManagement.Pages.Account;

#pragma warning disable CS0618
public class ProfileModel : PageModel
{
    private readonly EventManagementDbContext _context;
    private readonly ILogger<ProfileModel> _logger;
    private readonly AuthService _authService;
    private readonly GoogleCalendarService _googleCalendarService;
    private readonly IConfiguration _configuration;

    public ProfileModel(
        EventManagementDbContext context,
        ILogger<ProfileModel> logger,
        AuthService authService,
        GoogleCalendarService googleCalendarService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _authService = authService;
        _googleCalendarService = googleCalendarService;
        _configuration = configuration;
    }

    public User CurrentUser { get; set; } = default!;
    public List<string> UserRoles { get; set; } = new();
    public List<Event> RegisteredEvents { get; set; } = new();
    [BindProperty]
    public IFormFile? AvatarUpload { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/Account/Login");
        }

        var user = await _context.Users
            .Include(u => u.UserRoleUsers)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.RegistrationAttendees)
                .ThenInclude(r => r.Event)
            .FirstOrDefaultAsync(u => u.UserId == userId.Value);

        if (user == null)
        {
            return NotFound();
        }

        CurrentUser = user;
        UserRoles = user.UserRoleUsers
            .Where(ur => ur.IsActive)
            .Select(ur => ur.Role.RoleName)
            .ToList();
        RegisteredEvents = user.RegistrationAttendees
            .Where(r => !r.IsDeleted && r.Event != null)
            .Select(r => r.Event)
            .Distinct()
            .ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/Account/Login");
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return NotFound();
        }

        if (await TryUpdateModelAsync(user, "User",
            u => u.FullName, u => u.PhoneNumber))
        {
            // Xử lý upload avatar nếu có
            if (AvatarUpload != null && AvatarUpload.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(AvatarUpload.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["ErrorMessage"] = "Chỉ cho phép file ảnh JPG, PNG, GIF.";
                    return RedirectToPage();
                }
                if (AvatarUpload.Length > 2 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Ảnh đại diện không được vượt quá 2MB.";
                    return RedirectToPage();
                }
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var fileName = $"avatar_{user.UserId}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarUpload.CopyToAsync(stream);
                }
                user.ProfileImageUrl = $"/uploads/avatars/{fileName}";
            }
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/Account/Login");
        }

        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin.";
            return RedirectToPage();
        }

        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] = "Mật khẩu mới không khớp.";
            return RedirectToPage();
        }

        var success = await _authService.ChangePasswordAsync(userId.Value, currentPassword, newPassword);
        if (!success)
        {
            TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng.";
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
        return RedirectToPage();
    }

    public IActionResult OnGetConnectGoogleCalendar(int eventId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập lại!";
            return RedirectToPage("/Account/Login");
        }
        if (eventId <= 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn sự kiện để đồng bộ.";
            return RedirectToPage();
        }
        var state = $"{userId.Value}|{eventId}";
        var authUrl = _googleCalendarService.GetAuthorizationUrl(state);
        if (string.IsNullOrEmpty(authUrl))
        {
            TempData["ErrorMessage"] = "Không tạo được link xác thực Google. Kiểm tra lại cấu hình ClientId/Secret/RedirectUri!";
            return RedirectToPage();
        }
        _logger.LogInformation($"[GoogleCalendar] Redirecting to: {authUrl}");
        return Redirect(authUrl);
    }

    public async Task<IActionResult> OnGetGoogleCalendarCallback(string code, string state)
    {
        if (string.IsNullOrEmpty(state) || !state.Contains("|"))
        {
            TempData["ErrorMessage"] = "Thiếu thông tin sự kiện khi xác thực Google Calendar.";
            return RedirectToPage();
        }
        var parts = state.Split('|');
        var userId = int.Parse(parts[0]);
        var eventId = int.Parse(parts[1]);
        var token = await _googleCalendarService.ExchangeCodeForTokenAsync(code);
        var @event = await _context.Events.FindAsync(eventId);
        if (@event == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sự kiện để đồng bộ.";
            return RedirectToPage();
        }
        // Tạo event Google Calendar
        var googleEvent = new Google.Apis.Calendar.v3.Data.Event
        {
            Summary = @event.EventName,
            Description = @event.Description,
            Start = new Google.Apis.Calendar.v3.Data.EventDateTime { DateTime = @event.StartDate },
            End = new Google.Apis.Calendar.v3.Data.EventDateTime { DateTime = @event.EndDate },
            Location = @event.Location
        };
        try
        {
            var createdEvent = await _googleCalendarService.CreateGoogleEventAsync(token.AccessToken, googleEvent);
            var existingSync = await _context.CalendarSyncs.FirstOrDefaultAsync(s => s.UserId == userId && s.EventId == eventId && s.Provider == "Google");
            if (existingSync != null)
            {
                existingSync.SyncToken = token.AccessToken;
                existingSync.LastSyncedAt = DateTime.Now;
                existingSync.SyncStatus = "Success";
                existingSync.IsActive = true;
                existingSync.ExternalEventId = createdEvent.Id;
            }
            else
            {
                var sync = new CalendarSync
                {
                    UserId = userId,
                    EventId = eventId,
                    Provider = "Google",
                    SyncToken = token.AccessToken,
                    LastSyncedAt = DateTime.Now,
                    SyncStatus = "Success",
                    IsActive = true,
                    ExternalEventId = createdEvent.Id
                };
                _context.CalendarSyncs.Add(sync);
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đồng bộ sự kiện lên Google Calendar thành công!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi khi đồng bộ sự kiện lên Google Calendar: {ex.Message}";
        }
        return RedirectToPage();
    }
}
#pragma warning restore CS0618 