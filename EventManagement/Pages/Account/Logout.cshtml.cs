using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventManagement.Pages.Account;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        // Allow GET logout for convenience
        return await OnPostAsync();
    }

    public async Task<IActionResult> OnPostAsync()
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
            // Log error in production
            TempData["ErrorMessage"] = "An error occurred during logout. Please try again.";
            return RedirectToPage("/Index");
        }
    }
} 