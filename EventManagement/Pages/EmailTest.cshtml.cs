using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Services;

namespace EventManagement.Pages;

public class EmailTestModel : PageModel
{
    private readonly EmailService _emailService;

    public EmailTestModel(EmailService emailService)
    {
        _emailService = emailService;
    }

    [BindProperty]
    public string ToEmail { get; set; } = string.Empty;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var result = await _emailService.SendEmailAsync(
                ToEmail, 
                "🧪 Test Email", 
                "<h2>Email hoạt động rồi! 🎉</h2><p>Từ EventManagement System</p>");
            
            ViewData["Result"] = result ? "✅ Email sent!" : "❌ Failed to send";
        }
        catch (Exception ex)
        {
            ViewData["Result"] = $"❌ Error: {ex.Message}";
        }
        
        return Page();
    }
} 