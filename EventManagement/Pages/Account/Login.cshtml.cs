using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models.ViewModels;
using EventManagement.Services;
using System.ComponentModel.DataAnnotations;

namespace EventManagement.Pages.Account;

public class LoginModel : PageModel
{
    private readonly AuthService _authService;

    public LoginModel(AuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;

        public string? ReturnUrl { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        Input.ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _authService.ValidateUserAsync(Input.Email, Input.Password);
        
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        // Update last login
        await _authService.UpdateLastLoginAsync(user.UserId);

        // Store user info in session
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("UserName", user.FullName);
        HttpContext.Session.SetString("UserEmail", user.Email);
        
        var primaryRole = _authService.GetPrimaryUserRole(user);
        HttpContext.Session.SetString("UserRole", primaryRole);
        HttpContext.Session.SetString("UserEmailVerified", user.IsEmailVerified ? "true" : "false");

        // Set session timeout based on RememberMe
        if (Input.RememberMe)
        {
            HttpContext.Session.SetString("RememberMe", "true");
        }

        TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";

        // Redirect to return URL or default page
        if (!string.IsNullOrEmpty(Input.ReturnUrl) && Url.IsLocalUrl(Input.ReturnUrl))
        {
            return Redirect(Input.ReturnUrl);
        }
        if (primaryRole == "Admin")
        {
            return RedirectToPage("/Admin/Dashboard");
        }
        return RedirectToPage("/Index");
    }
} 