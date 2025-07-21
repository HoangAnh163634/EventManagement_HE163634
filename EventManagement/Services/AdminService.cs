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

    public async Task<(List<Registration>, int)> GetRegistrationsAsync(
        string? searchTerm = null,
        int? eventId = null,
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
            var query = _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.Attendee)
                .Include(r => r.CheckInByNavigation)
                .Where(r => !r.IsDeleted)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(r => 
                    r.Attendee.FullName.ToLower().Contains(searchTerm) ||
                    r.Attendee.Email.ToLower().Contains(searchTerm));
            }

            // Lọc theo sự kiện
            if (eventId.HasValue)
            {
                query = query.Where(r => r.EventId == eventId);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            // Lọc theo ngày
            if (startDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt.Date >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt.Date <= endDate.Value.Date);
            }

            // Đếm tổng số item
            var totalItems = await query.CountAsync();

            // Sắp xếp
            query = sortBy.ToLower() switch
            {
                "attendee" => sortOrder == "asc"
                    ? query.OrderBy(r => r.Attendee.FullName)
                    : query.OrderByDescending(r => r.Attendee.FullName),
                "event" => sortOrder == "asc"
                    ? query.OrderBy(r => r.Event.EventName)
                    : query.OrderByDescending(r => r.Event.EventName),
                _ => sortOrder == "asc"
                    ? query.OrderBy(r => r.CreatedAt)
                    : query.OrderByDescending(r => r.CreatedAt)
            };

            // Phân trang
            var registrations = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (registrations, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registrations list");
            throw;
        }
    }

    public async Task<Registration?> GetRegistrationByIdAsync(int registrationId)
    {
        try
        {
            return await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.Attendee)
                .Include(r => r.CheckInByNavigation)
                .Include(r => r.Qrcode)
                .Include(r => r.Feedback)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId && !r.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registration {RegistrationId}", registrationId);
            throw;
        }
    }

    public async Task UpdateRegistrationStatusAsync(int registrationId, string status)
    {
        try
        {
            var registration = await _context.Registrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

            if (registration == null)
            {
                throw new InvalidOperationException("Không tìm thấy đăng ký.");
            }

            if (registration.IsDeleted)
            {
                throw new InvalidOperationException("Đăng ký đã bị xóa.");
            }

            if (registration.Status == status)
            {
                return;
            }

            if (registration.Status == "CheckedIn")
            {
                throw new InvalidOperationException("Không thể thay đổi trạng thái của đăng ký đã check-in.");
            }

            if (status == "Confirmed" && registration.Event.MaxAttendees.HasValue)
            {
                var confirmedCount = await _context.Registrations
                    .CountAsync(r => r.EventId == registration.EventId 
                        && !r.IsDeleted 
                        && (r.Status == "Confirmed" || r.Status == "CheckedIn"));

                if (confirmedCount >= registration.Event.MaxAttendees)
                {
                    throw new InvalidOperationException("Sự kiện đã đạt số lượng người tham gia tối đa.");
                }
            }

            registration.Status = status;
            registration.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating registration {RegistrationId} status", registrationId);
            throw;
        }
    }

    public async Task CheckInRegistrationAsync(int registrationId, int checkInBy, string checkInMethod, string checkInLocation)
    {
        try
        {
            var registration = await _context.Registrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

            if (registration == null)
            {
                throw new InvalidOperationException("Không tìm thấy đăng ký.");
            }

            if (registration.IsDeleted)
            {
                throw new InvalidOperationException("Đăng ký đã bị xóa.");
            }

            if (registration.Status != "Confirmed")
            {
                throw new InvalidOperationException("Chỉ có thể check-in cho đăng ký đã được duyệt.");
            }

            if (registration.CheckInTime.HasValue)
            {
                throw new InvalidOperationException("Đăng ký đã được check-in trước đó.");
            }

            var now = DateTime.Now;
            if (now < registration.Event.StartDate.AddHours(-1))
            {
                throw new InvalidOperationException("Chỉ có thể check-in trong vòng 1 giờ trước khi sự kiện bắt đầu.");
            }

            if (now > registration.Event.EndDate)
            {
                throw new InvalidOperationException("Không thể check-in sau khi sự kiện kết thúc.");
            }

            registration.Status = "CheckedIn";
            registration.CheckInTime = now;
            registration.CheckInBy = checkInBy;
            registration.CheckInMethod = checkInMethod;
            registration.CheckInLocation = checkInLocation;
            registration.UpdatedAt = now;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in registration {RegistrationId}", registrationId);
            throw;
        }
    }

    public async Task<(List<Feedback>, int)> GetFeedbacksAsync(
        string? searchTerm = null,
        int? eventId = null,
        int? rating = null,
        bool? isApproved = null,
        bool? isPublic = null,
        string sortBy = "date",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            var query = _context.Feedbacks
                .Include(f => f.Event)
                .Include(f => f.Attendee)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(f => 
                    (f.Comments != null && f.Comments.ToLower().Contains(searchTerm)) ||
                    (f.Suggestions != null && f.Suggestions.ToLower().Contains(searchTerm)));
            }

            // Lọc theo sự kiện
            if (eventId.HasValue)
            {
                query = query.Where(f => f.EventId == eventId);
            }

            // Lọc theo đánh giá
            if (rating.HasValue)
            {
                query = query.Where(f => f.Rating == rating);
            }

            // Lọc theo trạng thái duyệt
            if (isApproved.HasValue)
            {
                query = query.Where(f => f.IsApproved == isApproved);
            }

            // Lọc theo trạng thái hiển thị
            if (isPublic.HasValue)
            {
                query = query.Where(f => f.IsPublic == isPublic);
            }

            // Đếm tổng số item
            var totalItems = await query.CountAsync();

            // Sắp xếp
            query = sortBy.ToLower() switch
            {
                "event" => sortOrder == "asc"
                    ? query.OrderBy(f => f.Event.EventName)
                    : query.OrderByDescending(f => f.Event.EventName),
                "attendee" => sortOrder == "asc"
                    ? query.OrderBy(f => f.Attendee.FullName)
                    : query.OrderByDescending(f => f.Attendee.FullName),
                "rating" => sortOrder == "asc"
                    ? query.OrderBy(f => f.Rating)
                    : query.OrderByDescending(f => f.Rating),
                _ => sortOrder == "asc"
                    ? query.OrderBy(f => f.SubmittedAt)
                    : query.OrderByDescending(f => f.SubmittedAt)
            };

            // Phân trang
            var feedbacks = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (feedbacks, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedbacks list");
            throw;
        }
    }

    public async Task<Feedback?> GetFeedbackByIdAsync(int feedbackId)
    {
        try
        {
            return await _context.Feedbacks
                .Include(f => f.Event)
                .Include(f => f.Attendee)
                .Include(f => f.Registration)
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback {FeedbackId}", feedbackId);
            throw;
        }
    }

    public async Task UpdateFeedbackStatusAsync(int feedbackId, bool isApproved, bool isPublic)
    {
        try
        {
            var feedback = await _context.Feedbacks.FindAsync(feedbackId);
            if (feedback == null)
            {
                throw new InvalidOperationException("Không tìm thấy feedback.");
            }

            feedback.IsApproved = isApproved;
            feedback.IsPublic = isPublic;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feedback {FeedbackId} status", feedbackId);
            throw;
        }
    }

    public async Task<(List<Notification>, int)> GetNotificationsAsync(
        string? searchTerm = null,
        int? eventId = null,
        string? notificationType = null,
        string? priority = null,
        string? status = null,
        string sortBy = "date",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            var query = _context.Notifications
                .Include(n => n.Event)
                .Include(n => n.User)
                .Include(n => n.SentByNavigation)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(n => 
                    n.Title.ToLower().Contains(searchTerm) ||
                    n.Subject.ToLower().Contains(searchTerm) ||
                    n.Body.ToLower().Contains(searchTerm));
            }

            // Lọc theo sự kiện
            if (eventId.HasValue)
            {
                query = query.Where(n => n.EventId == eventId);
            }

            // Lọc theo loại thông báo
            if (!string.IsNullOrWhiteSpace(notificationType))
            {
                query = query.Where(n => n.NotificationType == notificationType);
            }

            // Lọc theo mức độ ưu tiên
            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(n => n.Priority == priority);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(n => n.Status == status);
            }

            // Đếm tổng số item
            var totalItems = await query.CountAsync();

            // Sắp xếp
            query = sortBy.ToLower() switch
            {
                "title" => sortOrder == "asc"
                    ? query.OrderBy(n => n.Title)
                    : query.OrderByDescending(n => n.Title),
                "user" => sortOrder == "asc"
                    ? query.OrderBy(n => n.User.FullName)
                    : query.OrderByDescending(n => n.User.FullName),
                _ => sortOrder == "asc"
                    ? query.OrderBy(n => n.SentAt)
                    : query.OrderByDescending(n => n.SentAt)
            };

            // Phân trang
            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (notifications, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications list");
            throw;
        }
    }

    public async Task<Notification?> GetNotificationByIdAsync(int notificationId)
    {
        try
        {
            return await _context.Notifications
                .Include(n => n.Event)
                .Include(n => n.User)
                .Include(n => n.SentByNavigation)
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task<Notification> CreateNotificationAsync(
        string notificationType,
        string priority,
        int? eventId,
        int userId,
        string title,
        string subject,
        string body,
        string? link,
        int sentBy)
    {
        try
        {
            var notification = new Notification
            {
                NotificationType = notificationType,
                Priority = priority,
                EventId = eventId,
                UserId = userId,
                Title = title,
                Subject = subject,
                Body = body,
                Link = link,
                SentBy = sentBy,
                Status = "Sent",
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            throw;
        }
    }

    public async Task ResendNotificationAsync(int notificationId)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                throw new InvalidOperationException("Không tìm thấy thông báo.");
            }

            notification.Status = "Sent";
            notification.RetryCount++;
            notification.ErrorMessage = null;
            notification.SentAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending notification {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task DeleteNotificationAsync(int notificationId)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                throw new InvalidOperationException("Không tìm thấy thông báo.");
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task<(List<Qrcode>, int)> GetQRCodesAsync(
        string? searchTerm = null,
        int? eventId = null,
        bool? isActive = null,
        bool? isUsed = null,
        string? registrationStatus = null,
        string sortBy = "date",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            var query = _context.Qrcodes
                .Include(q => q.Event)
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Attendee)
                .Include(q => q.UsedByNavigation)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(q => q.QrcodeValue.ToLower().Contains(searchTerm));
            }

            // Lọc theo sự kiện
            if (eventId.HasValue)
            {
                query = query.Where(q => q.EventId == eventId);
            }

            // Lọc theo trạng thái hoạt động
            if (isActive.HasValue)
            {
                query = query.Where(q => q.IsActive == isActive);
            }

            // Lọc theo trạng thái sử dụng
            if (isUsed.HasValue)
            {
                query = query.Where(q => isUsed.Value ? q.UsedAt != null : q.UsedAt == null);
            }

            // Lọc theo trạng thái đăng ký
            if (!string.IsNullOrWhiteSpace(registrationStatus))
            {
                query = query.Where(q => q.Registration.Status == registrationStatus);
            }

            // Đếm tổng số item
            var totalItems = await query.CountAsync();

            // Sắp xếp
            query = sortBy.ToLower() switch
            {
                "event" => sortOrder == "asc"
                    ? query.OrderBy(q => q.Event.EventName)
                    : query.OrderByDescending(q => q.Event.EventName),
                "attendee" => sortOrder == "asc"
                    ? query.OrderBy(q => q.Registration.Attendee.FullName)
                    : query.OrderByDescending(q => q.Registration.Attendee.FullName),
                _ => sortOrder == "asc"
                    ? query.OrderBy(q => q.GeneratedAt)
                    : query.OrderByDescending(q => q.GeneratedAt)
            };

            // Phân trang
            var qrcodes = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (qrcodes, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QR codes list");
            throw;
        }
    }

    public async Task<Qrcode?> GetQRCodeByIdAsync(int qrcodeId)
    {
        try
        {
            return await _context.Qrcodes
                .Include(q => q.Event)
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Attendee)
                .Include(q => q.UsedByNavigation)
                .FirstOrDefaultAsync(q => q.QrcodeId == qrcodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QR code {QRCodeId}", qrcodeId);
            throw;
        }
    }

    public async Task<List<Registration>> GetRegistrationsForQRCodeAsync(int eventId)
    {
        try
        {
            return await _context.Registrations
                .Include(r => r.Attendee)
                .Where(r => r.EventId == eventId && r.Status == "Confirmed" && !r.IsDeleted)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registrations for QR code");
            throw;
        }
    }

    public async Task<Qrcode> GenerateQRCodeAsync(int eventId, int registrationId, DateTime? expiresAt)
    {
        try
        {
            var registration = await _context.Registrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

            if (registration == null)
            {
                throw new InvalidOperationException("Không tìm thấy đăng ký.");
            }

            if (registration.EventId != eventId)
            {
                throw new InvalidOperationException("Đăng ký không thuộc sự kiện này.");
            }

            if (registration.Status != "Confirmed")
            {
                throw new InvalidOperationException("Chỉ có thể tạo QR Code cho đăng ký đã được duyệt.");
            }

            if (registration.IsDeleted)
            {
                throw new InvalidOperationException("Đăng ký đã bị xóa.");
            }

            var existingQRCode = await _context.Qrcodes
                .FirstOrDefaultAsync(q => q.RegistrationId == registrationId && q.IsActive);

            if (existingQRCode != null)
            {
                throw new InvalidOperationException("Đã tồn tại QR Code đang hoạt động cho đăng ký này.");
            }

            var qrcode = new Qrcode
            {
                EventId = eventId,
                RegistrationId = registrationId,
                QrcodeValue = Guid.NewGuid().ToString(),
                QrcodeImageUrl = $"/qrcodes/{Guid.NewGuid()}.png", // Tạm thời, cần cập nhật sau khi tạo ảnh
                GeneratedAt = DateTime.Now,
                ExpiresAt = expiresAt,
                IsActive = true,
                ScanCount = 0
            };

            _context.Qrcodes.Add(qrcode);
            await _context.SaveChangesAsync();

            return qrcode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code");
            throw;
        }
    }

    public async Task ScanQRCodeAsync(string qrcodeValue, int userId, string checkInMethod, string checkInLocation)
    {
        try
        {
            var qrcode = await _context.Qrcodes
                .Include(q => q.Registration)
                .Include(q => q.Event)
                .FirstOrDefaultAsync(q => q.QrcodeValue == qrcodeValue);

            if (qrcode == null)
            {
                throw new InvalidOperationException("Không tìm thấy QR Code.");
            }

            if (!qrcode.IsActive)
            {
                throw new InvalidOperationException("QR Code đã hết hiệu lực.");
            }

            if (qrcode.UsedAt.HasValue)
            {
                throw new InvalidOperationException("QR Code đã được sử dụng.");
            }

            if (qrcode.ExpiresAt.HasValue && qrcode.ExpiresAt.Value < DateTime.Now)
            {
                throw new InvalidOperationException("QR Code đã hết hạn.");
            }

            var now = DateTime.Now;
            if (now < qrcode.Event.StartDate.AddHours(-1))
            {
                throw new InvalidOperationException("Chỉ có thể check-in trong vòng 1 giờ trước khi sự kiện bắt đầu.");
            }

            if (now > qrcode.Event.EndDate)
            {
                throw new InvalidOperationException("Không thể check-in sau khi sự kiện kết thúc.");
            }

            qrcode.UsedAt = now;
            qrcode.UsedBy = userId;
            qrcode.ScanCount++;

            qrcode.Registration.Status = "CheckedIn";
            qrcode.Registration.CheckInTime = now;
            qrcode.Registration.CheckInBy = userId;
            qrcode.Registration.CheckInMethod = checkInMethod;
            qrcode.Registration.CheckInLocation = checkInLocation;
            qrcode.Registration.UpdatedAt = now;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning QR code");
            throw;
        }
    }

    public async Task DeactivateQRCodeAsync(int qrcodeId)
    {
        try
        {
            var qrcode = await _context.Qrcodes.FindAsync(qrcodeId);
            if (qrcode == null)
            {
                throw new InvalidOperationException("Không tìm thấy QR Code.");
            }

            if (!qrcode.IsActive)
            {
                throw new InvalidOperationException("QR Code đã bị vô hiệu hóa trước đó.");
            }

            if (qrcode.UsedAt.HasValue)
            {
                throw new InvalidOperationException("Không thể vô hiệu hóa QR Code đã sử dụng.");
            }

            qrcode.IsActive = false;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating QR code {QRCodeId}", qrcodeId);
            throw;
        }
    }

    public async Task<(List<CalendarSync>, int)> GetCalendarSyncsAsync(
        string? searchTerm = null,
        int? eventId = null,
        string? provider = null,
        string? syncStatus = null,
        bool? isActive = null,
        string sortBy = "date",
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            var query = _context.CalendarSyncs
                .Include(c => c.Event)
                .Include(c => c.User)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(c => 
                    c.ExternalCalendarId != null && c.ExternalCalendarId.ToLower().Contains(searchTerm) ||
                    c.ExternalEventId != null && c.ExternalEventId.ToLower().Contains(searchTerm));
            }

            // Lọc theo sự kiện
            if (eventId.HasValue)
            {
                query = query.Where(c => c.EventId == eventId);
            }

            // Lọc theo nhà cung cấp
            if (!string.IsNullOrWhiteSpace(provider))
            {
                query = query.Where(c => c.Provider == provider);
            }

            // Lọc theo trạng thái đồng bộ
            if (!string.IsNullOrWhiteSpace(syncStatus))
            {
                query = query.Where(c => c.SyncStatus == syncStatus);
            }

            // Lọc theo trạng thái hoạt động
            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive);
            }

            // Đếm tổng số item
            var totalItems = await query.CountAsync();

            // Sắp xếp
            query = sortBy.ToLower() switch
            {
                "event" => sortOrder == "asc"
                    ? query.OrderBy(c => c.Event.EventName)
                    : query.OrderByDescending(c => c.Event.EventName),
                "user" => sortOrder == "asc"
                    ? query.OrderBy(c => c.User.FullName)
                    : query.OrderByDescending(c => c.User.FullName),
                _ => sortOrder == "asc"
                    ? query.OrderBy(c => c.LastSyncedAt)
                    : query.OrderByDescending(c => c.LastSyncedAt)
            };

            // Phân trang
            var syncs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (syncs, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar syncs list");
            throw;
        }
    }

    public async Task<CalendarSync?> GetCalendarSyncByIdAsync(int syncId)
    {
        try
        {
            return await _context.CalendarSyncs
                .Include(c => c.Event)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.SyncId == syncId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar sync {SyncId}", syncId);
            throw;
        }
    }

    public async Task<CalendarSync> CreateCalendarSyncAsync(
        int eventId,
        int userId,
        string provider,
        string externalCalendarId)
    {
        try
        {
            var existingSync = await _context.CalendarSyncs
                .FirstOrDefaultAsync(c => 
                    c.EventId == eventId && 
                    c.UserId == userId && 
                    c.Provider == provider &&
                    c.IsActive);

            if (existingSync != null)
            {
                throw new InvalidOperationException("Đã tồn tại đồng bộ đang hoạt động cho sự kiện và người dùng này.");
            }

            var sync = new CalendarSync
            {
                EventId = eventId,
                UserId = userId,
                Provider = provider,
                ExternalCalendarId = externalCalendarId,
                LastSyncedAt = DateTime.Now,
                NextSyncAt = DateTime.Now.AddHours(1),
                SyncStatus = "Pending",
                RetryCount = 0,
                IsActive = true
            };

            _context.CalendarSyncs.Add(sync);
            await _context.SaveChangesAsync();

            return sync;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating calendar sync");
            throw;
        }
    }

    public async Task SyncCalendarAsync(int syncId)
    {
        try
        {
            var sync = await _context.CalendarSyncs
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.SyncId == syncId);

            if (sync == null)
            {
                throw new InvalidOperationException("Không tìm thấy đồng bộ.");
            }

            if (!sync.IsActive)
            {
                throw new InvalidOperationException("Đồng bộ đã bị dừng.");
            }

            // TODO: Thực hiện đồng bộ với calendar provider
            sync.LastSyncedAt = DateTime.Now;
            sync.NextSyncAt = DateTime.Now.AddHours(1);
            sync.SyncStatus = "Synced";
            sync.ErrorMessage = null;
            sync.RetryCount = 0;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing calendar {SyncId}", syncId);
            throw;
        }
    }

    public async Task ToggleCalendarSyncAsync(int syncId, bool isActive)
    {
        try
        {
            var sync = await _context.CalendarSyncs.FindAsync(syncId);
            if (sync == null)
            {
                throw new InvalidOperationException("Không tìm thấy đồng bộ.");
            }

            sync.IsActive = isActive;
            if (!isActive)
            {
                sync.NextSyncAt = null;
            }
            else
            {
                sync.NextSyncAt = DateTime.Now.AddHours(1);
                sync.SyncStatus = "Pending";
                sync.ErrorMessage = null;
                sync.RetryCount = 0;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling calendar sync {SyncId}", syncId);
            throw;
        }
    }
} 