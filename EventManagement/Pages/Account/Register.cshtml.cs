using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EventManagement.Models;
using System.Security.Cryptography;
using System.Text;

namespace EventManagement.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly EventManagementContext _context;

        public RegisterModel(EventManagementContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Models.RegisterModel Input { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (_context.Users.Any(u => u.Email == Input.Email))
                {
                    ModelState.AddModelError(string.Empty, "Email already exists.");
                    return Page();
                }

                // Create new user
                var user = new User
                {
                    FullName = Input.FullName,
                    Email = Input.Email,
                    PasswordHash = HashPassword(Input.Password),
                    PhoneNumber = Input.PhoneNumber,
                    RoleId = 2, // Default role (assuming 2 is for regular users)
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registration successful! Please login to continue.";
                return RedirectToPage("./Login");
            }

            return Page();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
} 