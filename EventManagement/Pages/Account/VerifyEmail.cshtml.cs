using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Services;
using EventManagement.Models;

namespace EventManagement.Pages.Account;

public class VerifyEmailModel : PageModel
{
    private readonly EventManagementDbContext _context;
    public bool Success { get; set; }

    public VerifyEmailModel(EventManagementDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync(string? token)
    {
        Success = false;
        if (string.IsNullOrEmpty(token)) return;
        var user = _context.Users.FirstOrDefault(u => u.EmailVerificationToken == token && !u.IsEmailVerified);
        if (user != null)
        {
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            await _context.SaveChangesAsync();
            Success = true;
        }
    }
} 