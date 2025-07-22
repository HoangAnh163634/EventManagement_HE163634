using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventManagement.Services;

namespace EventManagement.Pages.Admin
{
    public class ApproveOrganizersModel : PageModel
    {
        private readonly EventManagementDbContext _context;
        private readonly EmailService _emailService;
        public ApproveOrganizersModel(EventManagementDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public List<UserRole> PendingRequests { get; set; } = new();
        public List<UserRole> HistoryRequests { get; set; } = new();

        public async Task OnGetAsync()
        {
            var organizerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Organizer");
            if (organizerRole != null)
            {
                PendingRequests = await _context.UserRoles
                    .Include(ur => ur.User)
                    .Where(ur => ur.RoleId == organizerRole.RoleId && !ur.IsActive)
                    .OrderBy(ur => ur.AssignedAt)
                    .ToListAsync();
                // Lấy lịch sử: đã duyệt (IsActive=true) hoặc đã bị từ chối (IsActive=false nhưng không còn trong PendingRequests)
                HistoryRequests = await _context.UserRoles
                    .Include(ur => ur.User)
                    .Where(ur => ur.RoleId == organizerRole.RoleId && (ur.IsActive || !PendingRequests.Select(p => p.UserId).Contains(ur.UserId)))
                    .OrderByDescending(ur => ur.AssignedAt)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int userId)
        {
            var organizerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Organizer");
            if (organizerRole == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy vai trò Nhà tổ chức.";
                return RedirectToPage();
            }
            var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == organizerRole.RoleId && !ur.IsActive);
            if (userRole == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu hoặc đã được duyệt.";
                return RedirectToPage();
            }
            userRole.IsActive = true;
            await _context.SaveChangesAsync();
            // Gửi email thông báo cho user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                var subject = "Yêu cầu nâng cấp quyền Nhà tổ chức đã được duyệt";
                var body = $"<p>Chào {user.FullName},</p><p>Yêu cầu nâng cấp quyền Nhà tổ chức của bạn đã được admin duyệt. Bạn đã có thể tạo và quản lý sự kiện trên hệ thống.</p><p>Trân trọng,<br>Ban quản trị</p>";
                await _emailService.SendEmailAsync(user.Email, subject, body, user.FullName);
            }
            TempData["SuccessMessage"] = "Đã duyệt quyền Nhà tổ chức cho người dùng và gửi email thông báo.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int userId)
        {
            var organizerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Organizer");
            if (organizerRole == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy vai trò Nhà tổ chức.";
                return RedirectToPage();
            }
            var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == organizerRole.RoleId && !ur.IsActive);
            if (userRole == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu hoặc đã được xử lý.";
                return RedirectToPage();
            }
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã từ chối yêu cầu Nhà tổ chức.";
            return RedirectToPage();
        }
    }
} 