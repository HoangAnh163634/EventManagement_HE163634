using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Services;
using EventManagement.Models;

namespace EventManagement.Pages.Account;

public class ResendVerificationModel : PageModel
{
    private readonly EventManagementDbContext _context;
    private readonly EmailService _emailService;

    public ResendVerificationModel(EventManagementDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để gửi lại email xác thực.";
            return RedirectToPage("/Index");
        }

        var user = _context.Users.FirstOrDefault(u => u.UserId == userId.Value);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
            return RedirectToPage("/Index");
        }

        if (user.IsEmailVerified)
        {
            TempData["InfoMessage"] = "Tài khoản của bạn đã được xác thực.";
            return RedirectToPage("/Index");
        }

        if (string.IsNullOrEmpty(user.EmailVerificationToken))
        {
            user.EmailVerificationToken = Guid.NewGuid().ToString("N");
            await _context.SaveChangesAsync();
        }

        var verifyUrl = Url.Page(
            "/Account/VerifyEmail",
            pageHandler: null,
            values: new { token = user.EmailVerificationToken },
            protocol: Request.Scheme
        );

        var emailBody = $@"<p>Chào {user.FullName},</p>
<p>Vui lòng xác thực email bằng cách click vào link sau:</p>
<p><a href='{verifyUrl}'>{verifyUrl}</a></p>
<p>Nếu bạn không đăng ký, hãy bỏ qua email này.</p>";

        await _emailService.SendEmailAsync(user.Email, "Xác thực tài khoản Event Management", emailBody, user.FullName);
        
        TempData["SuccessMessage"] = "Email xác thực đã được gửi lại. Vui lòng kiểm tra hộp thư.";
        return RedirectToPage("/Index");
    }
} 