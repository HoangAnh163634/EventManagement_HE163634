using System.Security.Cryptography;
using System.Text;
using EventManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Services;

public class AuthService
{
    private readonly EventManagementDbContext _context;

    public AuthService(EventManagementDbContext context)
    {
        _context = context;
    }

    public async Task<User?> ValidateUserAsync(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive && !u.IsDeleted);

        if (user == null) return null;

        var hashedPassword = HashPassword(password);
        return user.PasswordHash == hashedPassword ? user : null;
    }

    public async Task<AuthResult> RegisterUserAsync(string fullName, string email, string password, string? phoneNumber = null)
    {
        // Normalize email
        email = email.Trim().ToLowerInvariant();
        
        // Check if user already exists (case-insensitive)
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
            return AuthResult.Failed("Email đã được sử dụng");

        var hashedPassword = HashPassword(password);
        
        // Normalize and trim inputs
        fullName = fullName.Trim();
        phoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        
        // Sinh token xác thực email
        var emailVerificationToken = Guid.NewGuid().ToString("N");

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = hashedPassword,
            PhoneNumber = phoneNumber,
            CreatedAt = DateTime.Now,
            IsActive = true,
            IsDeleted = false,
            IsEmailVerified = false,
            EmailVerificationToken = emailVerificationToken
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assign default "Attendee" role
        var attendeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Attendee");
        if (attendeeRole != null)
        {
            var userRole = new UserRole
            {
                UserId = user.UserId,
                RoleId = attendeeRole.RoleId,
                AssignedAt = DateTime.Now,
                IsActive = true
            };
            
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }

        return AuthResult.Successful(user.UserId);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive && !u.IsDeleted);
    }

    public async Task<bool> UpdateLastLoginAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.LastLoginAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var currentHashedPassword = HashPassword(currentPassword);
        if (user.PasswordHash != currentHashedPassword) return false;

        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    public List<string> GetUserRoles(User user)
    {
        return user.UserRoleUsers?.Where(ur => ur.IsActive)
                                  .Select(ur => ur.Role.RoleName)
                                  .ToList() ?? new List<string>();
    }

    public async Task<string> GetPrimaryUserRole(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return "Attendee";
        return GetPrimaryUserRole(user);
    }

    public string GetPrimaryUserRole(User user)
    {
        var roles = GetUserRoles(user);
        
        // Priority order: Admin > Organizer > Staff > Attendee
        if (roles.Contains("Admin")) return "Admin";
        if (roles.Contains("Organizer")) return "Organizer";
        if (roles.Contains("Staff")) return "Staff";
        return "Attendee";
    }

    public bool HasRole(User user, string roleName)
    {
        var userRoles = GetUserRoles(user);
        return userRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAdmin(User user)
    {
        return HasRole(user, "Admin");
    }

    public bool IsOrganizer(User user)
    {
        return HasRole(user, "Organizer") || IsAdmin(user);
    }

    public bool CanCreateEvents(User user)
    {
        return IsOrganizer(user);
    }

    public bool CanManageEvent(User user, Event eventItem)
    {
        return IsAdmin(user) || eventItem.OrganizerId == user.UserId;
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
} 