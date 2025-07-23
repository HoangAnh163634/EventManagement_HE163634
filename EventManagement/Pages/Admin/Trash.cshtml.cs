using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace EventManagement.Pages.Admin;

public class TrashModel : PageModel
{
    private readonly EventService _eventService;
    private readonly AdminService _adminService;
    private readonly RegistrationService _registrationService;
    private readonly ILogger<TrashModel> _logger;

    public TrashModel(EventService eventService, AdminService adminService, RegistrationService registrationService, ILogger<TrashModel> logger)
    {
        _eventService = eventService;
        _adminService = adminService;
        _registrationService = registrationService;
        _logger = logger;
    }

    public List<Event> DeletedEvents { get; set; } = new();
    public List<User> DeletedUsers { get; set; } = new();
    public List<Registration> DeletedRegistrations { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
            return RedirectToPage("/Index");
        }
        DeletedEvents = await _eventService.GetAllEventsAsync(true);
        DeletedEvents = DeletedEvents.Where(e => e.IsDeleted).ToList();
        DeletedUsers = await _adminService.GetDeletedUsersAsync();
        DeletedRegistrations = await _registrationService.GetDeletedRegistrationsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRestoreEventAsync([FromBody] int eventId)
    {
        try
        {
            var evt = await _eventService.GetEventByIdAsync(eventId, true);
            if (evt == null) return new JsonResult(new { success = false, message = "Không tìm thấy sự kiện." });
            evt.IsDeleted = false;
            evt.UpdatedAt = DateTime.UtcNow;
            evt.LastModified = DateTime.UtcNow;
            await _eventService.UpdateEventAsync(evt);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khôi phục sự kiện");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi khôi phục sự kiện." });
        }
    }
    public async Task<IActionResult> OnPostDeleteEventAsync([FromBody] int eventId)
    {
        try
        {
            var evt = await _eventService.GetEventByIdAsync(eventId, true);
            if (evt == null) return new JsonResult(new { success = false, message = "Không tìm thấy sự kiện." });
            await _eventService.DeleteEventPermanentlyAsync(eventId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xóa vĩnh viễn sự kiện");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi xóa sự kiện." });
        }
    }
    public async Task<IActionResult> OnPostRestoreUserAsync([FromBody] int userId)
    {
        try
        {
            var user = await _adminService.GetUserByIdAsync(userId, true);
            if (user == null) return new JsonResult(new { success = false, message = "Không tìm thấy người dùng." });
            user.IsDeleted = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _adminService.UpdateUserAsync(user);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khôi phục người dùng");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi khôi phục người dùng." });
        }
    }
    public async Task<IActionResult> OnPostDeleteUserAsync([FromBody] int userId)
    {
        try
        {
            await _adminService.DeleteUserPermanentlyAsync(userId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xóa vĩnh viễn người dùng");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi xóa người dùng." });
        }
    }
    public async Task<IActionResult> OnPostRestoreRegistrationAsync([FromBody] int registrationId)
    {
        try
        {
            var reg = await _registrationService.GetRegistrationByIdAsync(registrationId, true);
            if (reg == null) return new JsonResult(new { success = false, message = "Không tìm thấy đăng ký." });
            reg.IsDeleted = false;
            reg.UpdatedAt = DateTime.UtcNow;
            await _registrationService.UpdateRegistrationAsync(reg);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khôi phục đăng ký");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi khôi phục đăng ký." });
        }
    }
    public async Task<IActionResult> OnPostDeleteRegistrationAsync([FromBody] int registrationId)
    {
        try
        {
            await _registrationService.DeleteRegistrationPermanentlyAsync(registrationId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xóa vĩnh viễn đăng ký");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi xóa đăng ký." });
        }
    }
} 