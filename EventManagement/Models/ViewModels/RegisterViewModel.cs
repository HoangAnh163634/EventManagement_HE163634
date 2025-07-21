using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EventManagement.Models.ViewModels;

public class RegisterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, ErrorMessage = "Full name must be between 2 and 100 characters", MinimumLength = 2)]
    [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "Full name can only contain letters and spaces")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid email format")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, ErrorMessage = "Password must be between 8 and 100 characters", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Password must contain at least 8 characters with uppercase, lowercase, number and special character")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number")]
    [RegularExpression(@"^(\+84|0)[3|5|7|8|9][0-9]{8}$", ErrorMessage = "Please enter a valid Vietnamese phone number")]
    [StringLength(15, ErrorMessage = "Phone number must not exceed 15 characters")]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "You must agree to the terms and conditions to create an account")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions")]
    [Display(Name = "I agree to the Terms and Conditions")]
    public bool AgreeToTerms { get; set; } = false;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Trim and validate FullName
        if (!string.IsNullOrEmpty(FullName))
        {
            FullName = FullName.Trim();
            if (string.IsNullOrWhiteSpace(FullName))
            {
                results.Add(new ValidationResult("Full name cannot be empty or contain only spaces", new[] { nameof(FullName) }));
            }
            else if (FullName.Length < 2)
            {
                results.Add(new ValidationResult("Full name must be at least 2 characters long", new[] { nameof(FullName) }));
            }
            else if (HasConsecutiveSpaces(FullName))
            {
                results.Add(new ValidationResult("Full name cannot contain consecutive spaces", new[] { nameof(FullName) }));
            }
        }

        // Trim and validate Email
        if (!string.IsNullOrEmpty(Email))
        {
            Email = Email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(Email))
            {
                results.Add(new ValidationResult("Email cannot be empty or contain only spaces", new[] { nameof(Email) }));
            }
            else if (!IsValidEmailDomain(Email))
            {
                results.Add(new ValidationResult("Please use a valid email domain", new[] { nameof(Email) }));
            }
        }

        // Validate PhoneNumber if provided
        if (!string.IsNullOrEmpty(PhoneNumber))
        {
            PhoneNumber = PhoneNumber.Trim().Replace(" ", "").Replace("-", "");
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                PhoneNumber = null; // Allow empty phone number
            }
        }

        // Password strength validation
        if (!string.IsNullOrEmpty(Password))
        {
            if (ContainsWhitespace(Password))
            {
                results.Add(new ValidationResult("Password cannot contain spaces", new[] { nameof(Password) }));
            }
            if (IsCommonPassword(Password))
            {
                results.Add(new ValidationResult("Password is too common. Please choose a stronger password", new[] { nameof(Password) }));
            }
        }

        return results;
    }

    private static bool HasConsecutiveSpaces(string input)
    {
        return input.Contains("  ");
    }

    private static bool IsValidEmailDomain(string email)
    {
        var blockedDomains = new[] { "test.com", "example.com", "fake.com", "temp.com" };
        var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();
        return !string.IsNullOrEmpty(domain) && !blockedDomains.Contains(domain);
    }

    private static bool ContainsWhitespace(string input)
    {
        return input.Any(char.IsWhiteSpace);
    }

    private static bool IsCommonPassword(string password)
    {
        var commonPasswords = new[] { "password", "123456", "12345678", "qwerty", "abc123", "password123" };
        return commonPasswords.Contains(password.ToLowerInvariant());
    }
} 