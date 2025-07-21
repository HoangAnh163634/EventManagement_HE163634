using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EventManagement.Services;
using EventManagement.Models.ViewModels;

namespace EventManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly JwtService _jwtService;
    private readonly EmailService _emailService;

    public AuthController(AuthService authService, JwtService jwtService, EmailService emailService)
    {
        _authService = authService;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginApiRequest request)
    {
        try
        {
            var user = await _authService.ValidateUserAsync(request.Email, request.Password);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var role = await _authService.GetPrimaryUserRole(user.UserId);
            var token = _jwtService.GenerateToken(user, role);

            // Update last login
            await _authService.UpdateLastLoginAsync(user.UserId);

            return Ok(new LoginApiResponse
            {
                Token = token,
                User = new UserInfo
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = role,
                    ProfileImageUrl = user.ProfileImageUrl
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterApiRequest request)
    {
        try
        {
            var result = await _authService.RegisterUserAsync(request.FullName, request.Email, request.Password, request.PhoneNumber);
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            // Send welcome email (optional - không block registration)
            try
            {
                var verificationToken = Guid.NewGuid().ToString();
                await _emailService.SendWelcomeEmailAsync(request.Email, request.FullName, verificationToken);
            }
            catch
            {
                // Log error but don't fail registration
            }

            return Ok(new { message = "Registration successful", userId = result.UserId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var role = await _authService.GetPrimaryUserRole(userId);

            return Ok(new UserInfo
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = role,
                ProfileImageUrl = user.ProfileImageUrl,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpPost("test-email")]
    [Authorize]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            var result = await _emailService.SendEmailAsync(request.ToEmail, request.Subject, request.Body);
            return Ok(new { success = result, message = result ? "Email sent successfully" : "Failed to send email" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}

// API Request/Response Models
public class LoginApiRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterApiRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class LoginApiResponse
{
    public string Token { get; set; } = string.Empty;
    public UserInfo User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

public class UserInfo
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class TestEmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
} 