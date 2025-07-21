using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace EventManagement.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(ILogger<LogoutModel> logger)
    {
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        // Allow GET logout for convenience
        return OnPost();
    }

    public IActionResult OnPost()
    {
        try
        {
            // Clear all session data
            HttpContext.Session.Clear();

            // Add success message
            TempData["SuccessMessage"] = "You have been logged out successfully.";

            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            // Log error
            _logger.LogError(ex, "Error occurred during logout");
            TempData["ErrorMessage"] = "An error occurred during logout. Please try again.";
            return RedirectToPage("/Index");
        }
    }
} 