using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventManagement.Pages.Staff;

public class RegistrationsModel : PageModel
{
    private readonly RegistrationService _registrationService;
    private readonly EventService _eventService;
    private readonly ILogger<RegistrationsModel> _logger;

    public RegistrationsModel(
        RegistrationService registrationService,
        EventService eventService,
        ILogger<RegistrationsModel> logger)
    {
        _registrationService = registrationService;
        _eventService = eventService;
        _logger = logger;
    }

    public List<Registration> Registrations { get; set; } = new();
    public List<Event> Events { get; set; } = new();

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)]
    public int? EventId { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Chỉ cho phép Staff truy cập
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Staff")
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
            return RedirectToPage("/Index");
        }
        Events = await _eventService.GetEventsAsync();
        var allRegs = await _registrationService.GetAllRegistrationsAsync();
        var query = allRegs.AsQueryable();
        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var search = SearchTerm.ToLower();
            query = query.Where(r => r.Attendee.FullName.ToLower().Contains(search) || r.Attendee.Email.ToLower().Contains(search));
        }
        if (EventId.HasValue)
        {
            query = query.Where(r => r.EventId == EventId.Value);
        }
        if (!string.IsNullOrEmpty(Status))
        {
            query = query.Where(r => r.Status == Status);
        }
        TotalItems = query.Count();
        CurrentPage = Page;
        Registrations = query.OrderByDescending(r => r.RegistrationDate)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int registrationId)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Staff")
        {
            return new JsonResult(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
        }
        try
        {
            var reg = await _registrationService.GetRegistrationByIdAsync(registrationId);
            if (reg == null)
                return new JsonResult(new { success = false, message = "Không tìm thấy đăng ký." });
            if (reg.Status != "Pending")
                return new JsonResult(new { success = false, message = "Chỉ có thể duyệt đăng ký ở trạng thái Chờ duyệt." });
            reg.Status = "Confirmed";
            reg.UpdatedAt = DateTime.UtcNow;
            await _registrationService.UpdateRegistrationAsync(reg);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi duyệt đăng ký Staff");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi duyệt đăng ký." });
        }
    }

    public async Task<IActionResult> OnPostCancelAsync(int registrationId)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Staff")
        {
            return new JsonResult(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
        }
        try
        {
            var reg = await _registrationService.GetRegistrationByIdAsync(registrationId);
            if (reg == null)
                return new JsonResult(new { success = false, message = "Không tìm thấy đăng ký." });
            if (reg.Status == "Cancelled")
                return new JsonResult(new { success = false, message = "Đăng ký đã bị hủy." });
            reg.Status = "Cancelled";
            reg.UpdatedAt = DateTime.UtcNow;
            await _registrationService.UpdateRegistrationAsync(reg);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi hủy đăng ký Staff");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi hủy đăng ký." });
        }
    }

    public async Task<IActionResult> OnPostCheckInAsync(int registrationId)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userRole != "Staff" || userId == null)
        {
            return new JsonResult(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
        }
        try
        {
            var reg = await _registrationService.GetRegistrationByIdAsync(registrationId);
            if (reg == null)
                return new JsonResult(new { success = false, message = "Không tìm thấy đăng ký." });
            if (reg.Status != "Confirmed")
                return new JsonResult(new { success = false, message = "Chỉ có thể check-in đăng ký đã duyệt." });
            reg.Status = "CheckedIn";
            reg.CheckInTime = DateTime.UtcNow;
            reg.CheckInBy = userId;
            reg.CheckInMethod = "Manual";
            reg.UpdatedAt = DateTime.UtcNow;
            await _registrationService.UpdateRegistrationAsync(reg);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi check-in đăng ký Staff");
            return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi check-in." });
        }
    }
} 