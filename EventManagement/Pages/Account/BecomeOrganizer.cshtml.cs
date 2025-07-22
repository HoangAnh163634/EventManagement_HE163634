using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

namespace EventManagement.Pages.Account
{
    public class BecomeOrganizerModel : PageModel
    {
        private readonly EventManagementDbContext _context;
        public BecomeOrganizerModel(EventManagementDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để gửi yêu cầu.";
                return RedirectToPage("/Account/Login");
            }

            // Kiểm tra đã có yêu cầu chưa
            var organizerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Organizer");
            if (organizerRole == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy vai trò Nhà tổ chức.";
                return RedirectToPage();
            }

            var existing = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == organizerRole.RoleId);
            if (existing != null)
            {
                if (existing.IsActive)
                {
                    TempData["ErrorMessage"] = "Bạn đã là Nhà tổ chức.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Bạn đã gửi yêu cầu, vui lòng chờ admin duyệt.";
                }
                return RedirectToPage();
            }

            // Tạo yêu cầu mới
            var userRole = new UserRole
            {
                UserId = userId.Value,
                RoleId = organizerRole.RoleId,
                AssignedAt = DateTime.Now,
                IsActive = false,
                AssignedBy = null
            };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Yêu cầu của bạn đã được gửi. Vui lòng chờ admin duyệt.";
            return RedirectToPage();
        }
    }
} 