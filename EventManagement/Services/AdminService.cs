using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EventManagement.Models;
using EventManagement.Models.ViewModels;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventManagement.Services;

public class AdminService
{
    private readonly EventManagementDbContext _context;
    private readonly ILogger<AdminService> _logger;
    private readonly IConfiguration _configuration;
    private readonly EmailService _emailService;

    public AdminService(
        EventManagementDbContext context, 
        ILogger<AdminService> logger, 
        IConfiguration configuration,
        EmailService emailService)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _emailService = emailService;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync()
    {
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var last30Days = today.AddDays(-29);

        // User stats
        var users = await _context.Users.Where(u => !u.IsDeleted).ToListAsync();
        var events = await _context.Events.Where(e => !e.IsDeleted).ToListAsync();
        var registrations = await _context.Registrations.Where(r => !r.IsDeleted).ToListAsync();
        var feedbacks = await _context.Feedbacks.ToListAsync();

        var dashboard = new AdminDashboardViewModel
        {
            // User statistics
            TotalUsers = users.Count,
            NewUsersToday = users.Count(u => u.CreatedAt.Date == today),
            NewUsersThisWeek = users.Count(u => u.CreatedAt.Date >= weekStart),
            NewUsersThisMonth = users.Count(u => u.CreatedAt.Date >= monthStart),

            // Event statistics
            TotalEvents = events.Count,
            NewEventsToday = events.Count(e => e.CreatedAt.Date == today),
            NewEventsThisWeek = events.Count(e => e.CreatedAt.Date >= weekStart),
            NewEventsThisMonth = events.Count(e => e.CreatedAt.Date >= monthStart),
            UpcomingEvents = events.Count(e => e.Status == "Upcoming"),
            OngoingEvents = events.Count(e => e.Status == "Ongoing"),
            CompletedEvents = events.Count(e => e.Status == "Completed"),
            CancelledEvents = events.Count(e => e.Status == "Cancelled"),

            // Registration statistics
            TotalRegistrations = registrations.Count,
            NewRegistrationsToday = registrations.Count(r => r.RegistrationDate.Date == today),
            NewRegistrationsThisWeek = registrations.Count(r => r.RegistrationDate.Date >= weekStart),
            NewRegistrationsThisMonth = registrations.Count(r => r.RegistrationDate.Date >= monthStart),

            // Feedback statistics
            TotalFeedbacks = feedbacks.Count,
            NewFeedbacksToday = feedbacks.Count(f => f.SubmittedAt.Date == today),
            NewFeedbacksThisWeek = feedbacks.Count(f => f.SubmittedAt.Date >= weekStart),
            NewFeedbacksThisMonth = feedbacks.Count(f => f.SubmittedAt.Date >= monthStart),

            // Event status distribution
            EventStatusDistribution = events
                .GroupBy(e => e.Status)
                .ToDictionary(g => g.Key, g => g.Count()),

            // User role distribution
            UserRoleDistribution = await _context.UserRoles
                .Include(ur => ur.Role)
                .GroupBy(ur => ur.Role.RoleName)
                .ToDictionaryAsync(g => g.Key, g => g.Count()),

            // Growth charts data
            UserGrowth = users
                .Where(u => u.CreatedAt.Date >= last30Days)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new ChartDataPoint { Date = g.Key, Value = g.Count() })
                .OrderBy(x => x.Date)
                .ToList(),

            EventGrowth = events
                .Where(e => e.CreatedAt.Date >= last30Days)
                .GroupBy(e => e.CreatedAt.Date)
                .Select(g => new ChartDataPoint { Date = g.Key, Value = g.Count() })
                .OrderBy(x => x.Date)
                .ToList(),

            RegistrationGrowth = registrations
                .Where(r => r.RegistrationDate.Date >= last30Days)
                .GroupBy(r => r.RegistrationDate.Date)
                .Select(g => new ChartDataPoint { Date = g.Key, Value = g.Count() })
                .OrderBy(x => x.Date)
                .ToList(),

            // Top events by registrations
            TopEvents = events
                .OrderByDescending(e => e.Registrations.Count)
                .Take(5)
                .Select(e => new TopEventItem
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    RegistrationCount = e.Registrations.Count
                })
                .ToList(),

            // Top organizers by events
            TopOrganizers = users
                .OrderByDescending(u => u.Events.Count)
                .Take(5)
                .Select(u => new TopOrganizerItem
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    EventCount = u.Events.Count
                })
                .ToList()
        };

        // Fill in missing dates with zero values for growth charts
        FillMissingDates(dashboard.UserGrowth, last30Days);
        FillMissingDates(dashboard.EventGrowth, last30Days);
        FillMissingDates(dashboard.RegistrationGrowth, last30Days);

        return dashboard;
    }

    private void FillMissingDates(List<ChartDataPoint> points, DateTime startDate)
    {
        var allDates = Enumerable.Range(0, 30)
            .Select(i => startDate.AddDays(i).Date)
            .ToList();

        var existingDates = points.Select(p => p.Date).ToList();
        var missingDates = allDates.Except(existingDates);

        foreach (var date in missingDates)
        {
            points.Add(new ChartDataPoint { Date = date, Value = 0 });
        }

        points.Sort((a, b) => a.Date.CompareTo(b.Date));
    }

    public async Task<(List<User> users, int totalItems)> GetUsersAsync(
        string? searchTerm, string? role, bool? isActive, 
        DateTime? startDate, DateTime? endDate,
        string sortBy, string sortOrder,
        int page, int pageSize)
    {
        try
        {
            var query = _context.Users
                .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
                .Where(u => !u.IsDeleted)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u => 
                    u.FullName.ToLower().Contains(searchTerm) || 
                    u.Email.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => 
                    u.UserRoleUsers.Any(ur => ur.Role.RoleName == role));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(u => u.CreatedAt.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(u => u.CreatedAt.Date <= endDate.Value.Date);
            }

            // Get total count before applying pagination
            var totalItems = await query.CountAsync();

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "name" => sortOrder == "asc" 
                    ? query.OrderBy(u => u.FullName)
                    : query.OrderByDescending(u => u.FullName),
                
                "email" => sortOrder == "asc"
                    ? query.OrderBy(u => u.Email)
                    : query.OrderByDescending(u => u.Email),
                
                _ => sortOrder == "asc"
                    ? query.OrderBy(u => u.CreatedAt)
                    : query.OrderByDescending(u => u.CreatedAt)
            };

            // Apply pagination
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users list");
            throw;
        }
    }

    public async Task<List<User>> GetAllUsersForExportAsync(
        string? searchTerm, string? role, bool? isActive,
        DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var query = _context.Users
                .Include(u => u.UserRoleUsers)
                .ThenInclude(ur => ur.Role)
                .Where(u => !u.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u => 
                    u.FullName.ToLower().Contains(searchTerm) || 
                    u.Email.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => 
                    u.UserRoleUsers.Any(ur => ur.Role.RoleName == role));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(u => u.CreatedAt.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(u => u.CreatedAt.Date <= endDate.Value.Date);
            }

            return await query.OrderBy(u => u.FullName).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for export");
            throw;
        }
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        try
        {
            return await _context.Users
                .Include(u => u.UserRoleUsers)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Events)
                .Include(u => u.RegistrationAttendees)
                    .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(u => u.UserId == id && !u.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by id {UserId}", id);
            throw;
        }
    }

    public async Task UpdateUserStatusAsync(int userId, bool isActive)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsDeleted)
            {
                throw new InvalidOperationException("User not found");
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status for user {UserId}", userId);
            throw;
        }
    }

    public async Task<(List<Event>, int)> GetEventsAsync(
        string? searchTerm = null,
        int? eventTypeId = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string sortBy = "date",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            var query = _context.Events
                .Include(e => e.EventType)
                .Include(e => e.Organizer)
                .Include(e => e.Registrations)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(e => 
                    e.EventName.ToLower().Contains(searchTerm) ||
                    e.Description.ToLower().Contains(searchTerm));
            }

            // Lọc theo loại sự kiện
            if (eventTypeId.HasValue)
            {
                query = query.Where(e => e.EventTypeId == eventTypeId);
            }

            // Lọc theo trạng thái
            var now = DateTime.Now;
            switch (status?.ToLower())
            {
                case "upcoming":
                    query = query.Where(e => e.Status != "Cancelled" && e.StartDate > now);
                    break;
                case "ongoing":
                    query = query.Where(e => e.Status != "Cancelled" && e.StartDate <= now && e.EndDate >= now);
                    break;
                case "completed":
                    query = query.Where(e => e.Status != "Cancelled" && e.EndDate < now);
                    break;
                case "cancelled":
                    query = query.Where(e => e.Status == "Cancelled");
                    break;
            }

            // Lọc theo ngày
            if (startDate.HasValue)
            {
                query = query.Where(e => e.StartDate.Date >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                query = query.Where(e => e.EndDate.Date <= endDate.Value.Date);
            }

            // Đếm tổng số item
            var totalItems = await query.CountAsync();

            // Sắp xếp
            query = sortBy.ToLower() switch
            {
                "name" => sortOrder == "asc" 
                    ? query.OrderBy(e => e.EventName)
                    : query.OrderByDescending(e => e.EventName),
                _ => sortOrder == "asc"
                    ? query.OrderBy(e => e.StartDate)
                    : query.OrderByDescending(e => e.StartDate)
            };

            // Phân trang
            var events = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (events, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events list");
            throw;
        }
    }

    public async Task<Event?> GetEventByIdAsync(int eventId)
    {
        try
        {
            return await _context.Events
                .Include(e => e.EventType)
                .Include(e => e.Organizer)
                .Include(e => e.Registrations)
                    .ThenInclude(r => r.Attendee)
                .FirstOrDefaultAsync(e => e.EventId == eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event {EventId}", eventId);
            throw;
        }
    }

    public async Task UpdateEventStatusAsync(int eventId, bool isCancelled)
    {
        try
        {
            var evt = await _context.Events.FindAsync(eventId);
            if (evt == null)
            {
                throw new InvalidOperationException("Không tìm thấy sự kiện.");
            }

            if (evt.EndDate < DateTime.Now)
            {
                throw new InvalidOperationException("Không thể hủy sự kiện đã kết thúc.");
            }

            evt.Status = isCancelled ? "Cancelled" : "Upcoming";
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId} status", eventId);
            throw;
        }
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        try
        {
            return await _context.Roles
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all roles");
            throw;
        }
    }

    public async Task UpdateUserAsync(int userId, bool isActive, int[] selectedRoles)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoleUsers)
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

            if (user == null)
            {
                throw new InvalidOperationException("Không tìm thấy người dùng.");
            }

            // Cập nhật trạng thái
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            // Cập nhật vai trò
            user.UserRoleUsers.Clear();
            foreach (var roleId in selectedRoles)
            {
                user.UserRoleUsers.Add(new UserRole
                {
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            throw;
        }
    }

    public async Task<(bool success, string? newPassword)> ResetUserPasswordAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return (false, null);
            }

            // Tạo mật khẩu mới
            var newPassword = GenerateRandomPassword();
            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, newPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ResendVerificationEmailAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return false;
            }

            // Tạo token mới
            user.EmailVerificationToken = Guid.NewGuid().ToString("N");
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Gửi email xác thực
            var verificationUrl = $"{_configuration["BaseUrl"]}/Account/VerifyEmail?token={user.EmailVerificationToken}";
            var emailBody = $@"<p>Chào {user.FullName},</p>
<p>Vui lòng click vào link bên dưới để xác thực email của bạn:</p>
<p><a href='{verificationUrl}'>{verificationUrl}</a></p>
<p>Link xác thực này sẽ hết hạn sau 24 giờ.</p>
<p>Trân trọng,<br>Ban quản trị</p>";

            await _emailService.SendEmailAsync(
                user.Email,
                "Xác thực email",
                emailBody,
                user.FullName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email for user {UserId}", userId);
            throw;
        }
    }

    private string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
} 