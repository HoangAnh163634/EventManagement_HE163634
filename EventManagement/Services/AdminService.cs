using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EventManagement.Models;
using EventManagement.Models.ViewModels;
using System.Collections.Generic;

namespace EventManagement.Services;

public class AdminService
{
    private readonly EventManagementDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(EventManagementDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
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
                throw new InvalidOperationException("User not found");
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            // Update roles
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
} 