using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;

namespace EventManagement.Pages.Admin.EventTypes;

public class IndexModel : PageModel
{
    private readonly EventService _eventService;

    public IndexModel(EventService eventService)
    {
        _eventService = eventService;
    }

    public List<EventType> EventTypes { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        // Check if user is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
            return RedirectToPage("/Index");
        }

        EventTypes = await _eventService.GetEventTypesAsync(includeInactive: true);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string eventTypeName, string? description, string iconClass, string colorCode)
    {
        try
        {
            await _eventService.CreateEventTypeAsync(eventTypeName, description, iconClass, colorCode);
            TempData["SuccessMessage"] = "Thêm loại sự kiện mới thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm loại sự kiện. Vui lòng thử lại.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(int eventTypeId, string eventTypeName, string? description, string iconClass, string colorCode, bool isActive)
    {
        try
        {
            await _eventService.UpdateEventTypeAsync(eventTypeId, eventTypeName, description, iconClass, colorCode, isActive);
            TempData["SuccessMessage"] = "Cập nhật loại sự kiện thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật loại sự kiện. Vui lòng thử lại.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int eventTypeId)
    {
        try
        {
            await _eventService.DeleteEventTypeAsync(eventTypeId);
            TempData["SuccessMessage"] = "Xóa loại sự kiện thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa loại sự kiện. Vui lòng thử lại.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int eventTypeId, bool isActive)
    {
        try
        {
            await _eventService.UpdateEventTypeStatusAsync(eventTypeId, isActive);
            TempData["SuccessMessage"] = $"Đã {(isActive ? "kích hoạt" : "vô hiệu hóa")} loại sự kiện thành công.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi thay đổi trạng thái. Vui lòng thử lại.";
        }

        return RedirectToPage();
    }
} 