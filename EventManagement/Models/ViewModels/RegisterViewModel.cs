using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EventManagement.Models.ViewModels;

public class RegisterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Tên đầy đủ là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên đầy đủ phải từ 2 đến 100 ký tự", MinimumLength = 2)]
    [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "Tên chỉ được chứa chữ cái và khoảng trắng")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Định dạng email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, ErrorMessage = "Mật khẩu phải từ 8 đến 100 ký tự", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ")]
    [RegularExpression(@"^(\+84|0)[3|5|7|8|9][0-9]{8}$", ErrorMessage = "Vui lòng nhập số điện thoại Việt Nam hợp lệ")]
    [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Bạn phải đồng ý với điều khoản và điều kiện để tạo tài khoản")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn phải đồng ý với điều khoản và điều kiện")]
    [Display(Name = "Tôi đồng ý với Điều khoản & Chính sách")]
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