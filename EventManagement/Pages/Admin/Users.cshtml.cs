using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using EventManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text;
using ClosedXML.Excel;

namespace EventManagement.Pages.Admin;

public class UsersModel : PageModel
{
    private readonly AdminService _adminService;
    private readonly ILogger<UsersModel> _logger;

    public UsersModel(AdminService adminService, ILogger<UsersModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    public List<User> Users { get; set; } = new();
    public int TotalItems { get; set; }
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Role { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsActive { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "date";

    [BindProperty(SupportsGet = true)]
    public string SortOrder { get; set; } = "desc";

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
            var (users, totalItems) = await _adminService.GetUsersAsync(
                SearchTerm, Role, IsActive, StartDate, EndDate, 
                SortBy, SortOrder, CurrentPage, PageSize);

            Users = users;
            TotalItems = totalItems;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users list");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách người dùng.";
            return RedirectToPage("/Index");
        }
    }

    public async Task<IActionResult> OnPostToggleStatusAsync([FromBody] ToggleStatusModel model)
    {
        try
        {
            await _adminService.UpdateUserStatusAsync(model.UserId, model.IsActive);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status");
            return new JsonResult(new { success = false });
        }
    }

    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        try
        {
            var users = await _adminService.GetAllUsersForExportAsync(
                SearchTerm, Role, IsActive, StartDate, EndDate);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Users");

            // Add headers
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Họ tên";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Điện thoại";
            worksheet.Cell(1, 5).Value = "Vai trò";
            worksheet.Cell(1, 6).Value = "Trạng thái";
            worksheet.Cell(1, 7).Value = "Email đã xác thực";
            worksheet.Cell(1, 8).Value = "Ngày tạo";
            worksheet.Cell(1, 9).Value = "Lần đăng nhập cuối";

            // Add data
            var row = 2;
            foreach (var user in users)
            {
                worksheet.Cell(row, 1).Value = user.UserId;
                worksheet.Cell(row, 2).Value = user.FullName;
                worksheet.Cell(row, 3).Value = user.Email;
                worksheet.Cell(row, 4).Value = user.PhoneNumber;
                worksheet.Cell(row, 5).Value = string.Join(", ", user.UserRoleUsers.Select(ur => ur.Role.RoleName));
                worksheet.Cell(row, 6).Value = user.IsActive ? "Active" : "Inactive";
                worksheet.Cell(row, 7).Value = user.IsEmailVerified ? "Đã xác thực" : "Chưa xác thực";
                worksheet.Cell(row, 8).Value = user.CreatedAt;
                worksheet.Cell(row, 9).Value = user.LastLoginAt;
                row++;
            }

            // Style the header
            var header = worksheet.Range(1, 1, 1, 9);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.Gold;
            header.Style.Font.FontColor = XLColor.Black;

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Generate the file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Users_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to Excel");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xuất file Excel.";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        try
        {
            var users = await _adminService.GetAllUsersForExportAsync(
                SearchTerm, Role, IsActive, StartDate, EndDate);

            var csv = new StringBuilder();
            csv.AppendLine("ID,Họ tên,Email,Điện thoại,Vai trò,Trạng thái,Email đã xác thực,Ngày tạo,Lần đăng nhập cuối");

            foreach (var user in users)
            {
                csv.AppendLine($"{user.UserId}," +
                    $"\"{user.FullName}\"," +
                    $"\"{user.Email}\"," +
                    $"\"{user.PhoneNumber}\"," +
                    $"\"{string.Join(", ", user.UserRoleUsers.Select(ur => ur.Role.RoleName))}\"," +
                    $"{(user.IsActive ? "Active" : "Inactive")}," +
                    $"{(user.IsEmailVerified ? "Đã xác thực" : "Chưa xác thực")}," +
                    $"{user.CreatedAt:dd/MM/yyyy HH:mm}," +
                    $"{(user.LastLoginAt.HasValue ? user.LastLoginAt.Value.ToString("dd/MM/yyyy HH:mm") : "")}");
            }

            return File(
                Encoding.UTF8.GetBytes(csv.ToString()),
                "text/csv",
                $"Users_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to CSV");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xuất file CSV.";
            return RedirectToPage();
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
            { "role", Role },
            { "isActive", IsActive?.ToString() },
            { "startDate", StartDate?.ToString("yyyy-MM-dd") },
            { "endDate", EndDate?.ToString("yyyy-MM-dd") },
            { "page", "1" }
        };

        return $"{Request.Path}?{string.Join("&", queryParams.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"))}";
    }

    public string GetPageUrl(int pageNumber)
    {
        var queryParams = new Dictionary<string, string?>
        {
            { "currentPage", pageNumber.ToString() },
            { "searchTerm", SearchTerm },
            { "role", Role },
            { "isActive", IsActive?.ToString() },
            { "startDate", StartDate?.ToString("yyyy-MM-dd") },
            { "endDate", EndDate?.ToString("yyyy-MM-dd") },
            { "sortBy", SortBy },
            { "sortOrder", SortOrder }
        };
        return $"{Request.Path}?{string.Join("&", queryParams.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"))}";
    }
}

public class ToggleStatusModel
{
    public int UserId { get; set; }
    public bool IsActive { get; set; }
} 