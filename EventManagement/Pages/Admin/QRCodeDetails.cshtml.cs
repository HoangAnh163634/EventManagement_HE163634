using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Threading.Tasks;

namespace EventManagement.Pages.Admin;

public class QRCodeDetailsModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly ILogger<QRCodeDetailsModel> _logger;

    public QRCodeDetailsModel(
        AdminService adminService,
        ILogger<QRCodeDetailsModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public Qrcode? QRCode { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            // Kiểm tra phân quyền
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToPage("/Index");
            }

            // Lấy thông tin QR Code
            QRCode = await _adminService.GetQRCodeByIdAsync(id);
            if (QRCode == null)
            {
                return Page();
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading QR code details for QR code {QRCodeId}", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin QR Code.";
            return RedirectToPage("/Admin/QRCodes");
        }
    }
} 