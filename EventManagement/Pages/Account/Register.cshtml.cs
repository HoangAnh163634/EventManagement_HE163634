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
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name must be between 2 and 100 characters", MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be between 6 and 100 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "You must agree to the terms and conditions")]
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
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Registration failed.");
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
            _logger.LogError(ex, "Error occurred during user registration");
            ModelState.AddModelError(string.Empty, "An error occurred while creating your account. Please try again.");
            return Page();
        }
    }
} 