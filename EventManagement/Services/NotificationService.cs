using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using EventManagement.Models;
using EventManagement.Hubs;

namespace EventManagement.Services;

public class NotificationService
{
    private readonly EventManagementDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly EmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        EventManagementDbContext context,
        IHubContext<NotificationHub> hubContext,
        EmailService emailService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
    {
        return await _context.Notifications
            .Include(n => n.Event)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<int> GetUnreadNotificationCount(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return 0;
        }

        try
        {
            return await _context.Notifications
                .Where(n => n.UserId.ToString() == userId && !n.IsRead && n.Status != "Deleted")
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notification count for user {UserId}", userId);
            return 0;
        }
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task NotifyNewRegistrationAsync(Registration registration)
    {
        // Notify event organizer
        var notification = new Notification
        {
            UserId = registration.Event.OrganizerId,
            EventId = registration.EventId,
            NotificationType = "NewRegistration",
            Priority = "Normal",
            Title = "Đăng ký mới",
            Subject = "Có người đăng ký tham gia sự kiện",
            Body = $"{registration.Attendee.FullName} đã đăng ký tham gia sự kiện {registration.Event.EventName}.",
            Status = "Sent",
            SentAt = DateTime.UtcNow,
            SentBy = registration.AttendeeId,
            Link = $"/Events/Details/{registration.EventId}"
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _hubContext.Clients
            .Group($"User_{registration.Event.OrganizerId}")
            .SendAsync("ReceiveNotification", new
            {
                notification.NotificationId,
                notification.Title,
                notification.Body,
                notification.Link,
                notification.SentAt,
                SenderName = registration.Attendee.FullName,
                SenderAvatar = registration.Attendee.ProfileImageUrl
            });

        // Send email notification
        var organizer = await _context.Users.FindAsync(registration.Event.OrganizerId);
        if (organizer != null)
        {
            var emailBody = $@"<p>Chào {organizer.FullName},</p>
<p>{registration.Attendee.FullName} đã đăng ký tham gia sự kiện <strong>{registration.Event.EventName}</strong>.</p>
<p>Thông tin đăng ký:</p>
<ul>
    <li>Thời gian: {registration.RegistrationDate:dd/MM/yyyy HH:mm}</li>
    <li>Email: {registration.Attendee.Email}</li>
    <li>Số điện thoại: {registration.Attendee.PhoneNumber}</li>
</ul>
<p>Vui lòng kiểm tra và xác nhận đăng ký.</p>";

            await _emailService.SendEmailAsync(
                organizer.Email,
                $"Đăng ký mới: {registration.Event.EventName}",
                emailBody,
                organizer.FullName);
        }
    }

    public async Task NotifyCancelRegistrationAsync(Registration registration)
    {
        // Notify event organizer
        var notification = new Notification
        {
            UserId = registration.Event.OrganizerId,
            EventId = registration.EventId,
            NotificationType = "CancelRegistration",
            Priority = "Normal",
            Title = "Hủy đăng ký",
            Subject = "Có người hủy đăng ký tham gia sự kiện",
            Body = $"{registration.Attendee.FullName} đã hủy đăng ký tham gia sự kiện {registration.Event.EventName}.",
            Status = "Sent",
            SentAt = DateTime.UtcNow,
            SentBy = registration.AttendeeId,
            Link = $"/Events/Details/{registration.EventId}"
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _hubContext.Clients
            .Group($"User_{registration.Event.OrganizerId}")
            .SendAsync("ReceiveNotification", new
            {
                notification.NotificationId,
                notification.Title,
                notification.Body,
                notification.Link,
                notification.SentAt,
                SenderName = registration.Attendee.FullName,
                SenderAvatar = registration.Attendee.ProfileImageUrl
            });

        // Send email notification
        var organizer = await _context.Users.FindAsync(registration.Event.OrganizerId);
        if (organizer != null)
        {
            var emailBody = $@"<p>Chào {organizer.FullName},</p>
<p>{registration.Attendee.FullName} đã hủy đăng ký tham gia sự kiện <strong>{registration.Event.EventName}</strong>.</p>
<p>Thông tin hủy đăng ký:</p>
<ul>
    <li>Thời gian: {DateTime.UtcNow:dd/MM/yyyy HH:mm}</li>
    <li>Lý do: {registration.CancellationReason ?? "Không có"}</li>
</ul>";

            await _emailService.SendEmailAsync(
                organizer.Email,
                $"Hủy đăng ký: {registration.Event.EventName}",
                emailBody,
                organizer.FullName);
        }
    }

    public async Task NotifyEventUpdateAsync(Event evt, string updateType)
    {
        // Get all attendees
        var attendeeIds = await _context.Registrations
            .Where(r => r.EventId == evt.EventId && !r.IsDeleted)
            .Select(r => r.AttendeeId)
            .ToListAsync();

        // Create notifications
        var notifications = attendeeIds.Select(attendeeId => new Notification
        {
            UserId = attendeeId,
            EventId = evt.EventId,
            NotificationType = "EventUpdate",
            Priority = "Normal",
            Title = "Cập nhật sự kiện",
            Subject = "Sự kiện có thay đổi",
            Body = $"Sự kiện {evt.EventName} đã được cập nhật: {updateType}",
            Status = "Sent",
            SentAt = DateTime.UtcNow,
            SentBy = evt.OrganizerId,
            Link = $"/Events/Details/{evt.EventId}"
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Send real-time notifications
        foreach (var attendeeId in attendeeIds)
        {
            await _hubContext.Clients
                .Group($"User_{attendeeId}")
                .SendAsync("ReceiveNotification", new
                {
                    NotificationId = notifications.First(n => n.UserId == attendeeId).NotificationId,
                    Title = "Cập nhật sự kiện",
                    Body = $"Sự kiện {evt.EventName} đã được cập nhật: {updateType}",
                    Link = $"/Events/Details/{evt.EventId}",
                    SentAt = DateTime.UtcNow,
                    SenderName = evt.Organizer.FullName,
                    SenderAvatar = evt.Organizer.ProfileImageUrl
                });
        }

        // Send email notifications
        var attendees = await _context.Users
            .Where(u => attendeeIds.Contains(u.UserId))
            .ToListAsync();

        foreach (var attendee in attendees)
        {
            var emailBody = $@"<p>Chào {attendee.FullName},</p>
<p>Sự kiện <strong>{evt.EventName}</strong> đã được cập nhật.</p>
<p>Thay đổi: {updateType}</p>
<p>Vui lòng kiểm tra chi tiết sự kiện để biết thêm thông tin.</p>";

            await _emailService.SendEmailAsync(
                attendee.Email,
                $"Cập nhật sự kiện: {evt.EventName}",
                emailBody,
                attendee.FullName);
        }
    }

    public async Task NotifyEventCancelledAsync(Event evt)
    {
        // Get all attendees
        var attendeeIds = await _context.Registrations
            .Where(r => r.EventId == evt.EventId && !r.IsDeleted)
            .Select(r => r.AttendeeId)
            .ToListAsync();

        // Create notifications
        var notifications = attendeeIds.Select(attendeeId => new Notification
        {
            UserId = attendeeId,
            EventId = evt.EventId,
            NotificationType = "EventCancelled",
            Priority = "High",
            Title = "Sự kiện bị hủy",
            Subject = "Sự kiện đã bị hủy",
            Body = $"Sự kiện {evt.EventName} đã bị hủy.",
            Status = "Sent",
            SentAt = DateTime.UtcNow,
            SentBy = evt.OrganizerId,
            Link = $"/Events/Details/{evt.EventId}"
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Send real-time notifications
        foreach (var attendeeId in attendeeIds)
        {
            await _hubContext.Clients
                .Group($"User_{attendeeId}")
                .SendAsync("ReceiveNotification", new
                {
                    NotificationId = notifications.First(n => n.UserId == attendeeId).NotificationId,
                    Title = "Sự kiện bị hủy",
                    Body = $"Sự kiện {evt.EventName} đã bị hủy.",
                    Link = $"/Events/Details/{evt.EventId}",
                    SentAt = DateTime.UtcNow,
                    SenderName = evt.Organizer.FullName,
                    SenderAvatar = evt.Organizer.ProfileImageUrl
                });
        }

        // Send email notifications
        var attendees = await _context.Users
            .Where(u => attendeeIds.Contains(u.UserId))
            .ToListAsync();

        foreach (var attendee in attendees)
        {
            var emailBody = $@"<p>Chào {attendee.FullName},</p>
<p>Sự kiện <strong>{evt.EventName}</strong> đã bị hủy.</p>
<p>Thông tin sự kiện:</p>
<ul>
    <li>Thời gian: {evt.StartDate:dd/MM/yyyy HH:mm}</li>
    <li>Địa điểm: {evt.Location}</li>
</ul>
<p>Chúng tôi rất tiếc về sự bất tiện này.</p>";

            await _emailService.SendEmailAsync(
                attendee.Email,
                $"Sự kiện bị hủy: {evt.EventName}",
                emailBody,
                attendee.FullName);
        }
    }
} 