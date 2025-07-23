using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models.ViewModels;
using EventManagement.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace EventManagement.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly AuthService _authService;
    private readonly EmailService _emailService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(AuthService authService, EmailService emailService, ILogger<RegisterModel> logger)
    {
        _authService = authService;
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Tên đầy đủ là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên đầy đủ phải nằm giữa 2 và 100 ký tự", MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải nằm giữa 6 và 100 ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Bạn phải đồng ý với các điều khoản và điều kiện")]
        public bool AgreeToTerms { get; set; } = false;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await _authService.RegisterUserAsync(
                Input.FullName, 
                Input.Email, 
                Input.Password, 
                Input.PhoneNumber);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đăng ký thất bại.");
                return Page();
            }

            // Lấy user để lấy token xác thực
            var user = await _authService.GetUserByIdAsync(result.UserId!.Value);
            if (user != null && !string.IsNullOrEmpty(user.EmailVerificationToken))
            {
                var verifyUrl = Url.Page(
                    "/Account/VerifyEmail",
                    pageHandler: null,
                    values: new { token = user.EmailVerificationToken },
                    protocol: Request.Scheme
                );
                var emailBody = $@"<p>Chào {user.FullName},</p><p>Vui lòng xác thực email bằng cách click vào link sau:</p><p><a href='{verifyUrl}'>{verifyUrl}</a></p><p>Nếu bạn không đăng ký, hãy bỏ qua email này.</p>";
                _ = _emailService.SendEmailAsync(user.Email, "Xác thực tài khoản Event Management", emailBody, user.FullName);
            }

            TempData["SuccessMessage"] = "🎉 Tài khoản đã được tạo thành công! Kiểm tra email để xác thực tài khoản.";
            return RedirectToPage("/Account/Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đã xảy ra lỗi khi tạo tài khoản của bạn");
            ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo tài khoản của bạn. Vui lòng thử lại.");
            return Page();
        }
    }
} 