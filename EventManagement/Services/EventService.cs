using EventManagement.Models;
using EventManagement.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Services;

public class EventService
{
    private readonly EventManagementDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IEmailService _emailService; // Added this field

    public EventService(EventManagementDbContext context, IWebHostEnvironment environment, IEmailService emailService) // Added emailService to constructor
    {
        _context = context;
        _environment = environment;
        _emailService = emailService; // Initialize emailService
    }

    public async Task<List<Event>> GetAllEventsAsync(bool includeDeleted = false)
    {
        var query = _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(e => !e.IsDeleted);
        }

        return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        return await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .Include(e => e.Feedbacks)
            .FirstOrDefaultAsync(e => e.EventId == id && !e.IsDeleted);
    }

    public async Task<Event> CreateEventAsync(EventViewModel model, int organizerId)
    {
        var bannerUrl = await SaveBannerImageAsync(model.BannerImage);

        var newEvent = new Event
        {
            OrganizerId = organizerId,
            EventTypeId = model.EventTypeId,
            EventName = model.EventName,
            Description = model.Description,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Location = model.Location,
            Address = model.Address,
            IsPublic = model.IsPublic,
            PrivacyLevel = model.PrivacyLevel,
            MaxAttendees = model.MaxAttendees,
            Status = model.Status,
            RegistrationDeadline = model.RegistrationDeadline,
            Price = model.Price,
            Currency = model.Currency ?? "VND",
            BannerImageUrl = bannerUrl,
            Tags = model.Tags,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        return newEvent;
    }

    public async Task<Event> UpdateEventAsync(EventViewModel model)
    {
        var existingEvent = await _context.Events.FindAsync(model.EventId);
        if (existingEvent == null)
        {
            throw new InvalidOperationException("Event not found");
        }

        var bannerUrl = await SaveBannerImageAsync(model.BannerImage) ?? existingEvent.BannerImageUrl;

        existingEvent.EventTypeId = model.EventTypeId;
        existingEvent.EventName = model.EventName;
        existingEvent.Description = model.Description;
        existingEvent.StartDate = model.StartDate;
        existingEvent.EndDate = model.EndDate;
        existingEvent.Location = model.Location;
        existingEvent.Address = model.Address;
        existingEvent.IsPublic = model.IsPublic;
        existingEvent.PrivacyLevel = model.PrivacyLevel;
        existingEvent.MaxAttendees = model.MaxAttendees;
        existingEvent.Status = model.Status;
        existingEvent.RegistrationDeadline = model.RegistrationDeadline;
        existingEvent.Price = model.Price;
        existingEvent.Currency = model.Currency;
        existingEvent.BannerImageUrl = bannerUrl;
        existingEvent.Tags = model.Tags;
        existingEvent.UpdatedAt = DateTime.UtcNow;
        existingEvent.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingEvent;
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        var existingEvent = await _context.Events.FindAsync(id);
        if (existingEvent == null)
        {
            return false;
        }

        existingEvent.IsDeleted = true;
        existingEvent.UpdatedAt = DateTime.UtcNow;
        existingEvent.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<EventType>> GetEventTypesAsync(bool includeInactive = false)
    {
        var query = _context.EventTypes.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(et => et.IsActive);
        }
        return await query.OrderBy(et => et.EventTypeName).ToListAsync();
    }

    public async Task<EventType> CreateEventTypeAsync(string name, string? description, string iconClass, string colorCode)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Tên loại sự kiện không được để trống.");
        }

        // Check if name exists
        var exists = await _context.EventTypes.AnyAsync(et => et.EventTypeName == name);
        if (exists)
        {
            throw new InvalidOperationException("Tên loại sự kiện đã tồn tại.");
        }

        // Validate icon class
        if (string.IsNullOrWhiteSpace(iconClass))
        {
            throw new InvalidOperationException("Icon class không được để trống.");
        }

        // Validate color code
        if (string.IsNullOrWhiteSpace(colorCode) || !colorCode.StartsWith("#") || colorCode.Length != 7)
        {
            throw new InvalidOperationException("Mã màu không hợp lệ.");
        }

        var eventType = new EventType
        {
            EventTypeName = name,
            Description = description,
            IconClass = iconClass,
            ColorCode = colorCode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.EventTypes.Add(eventType);
        await _context.SaveChangesAsync();
        return eventType;
    }

    public async Task<EventType> UpdateEventTypeAsync(int id, string name, string? description, string iconClass, string colorCode, bool isActive)
    {
        var eventType = await _context.EventTypes.FindAsync(id);
        if (eventType == null)
        {
            throw new InvalidOperationException("Không tìm thấy loại sự kiện.");
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Tên loại sự kiện không được để trống.");
        }

        // Check if name exists (excluding current)
        var exists = await _context.EventTypes.AnyAsync(et => et.EventTypeName == name && et.EventTypeId != id);
        if (exists)
        {
            throw new InvalidOperationException("Tên loại sự kiện đã tồn tại.");
        }

        // Validate icon class
        if (string.IsNullOrWhiteSpace(iconClass))
        {
            throw new InvalidOperationException("Icon class không được để trống.");
        }

        // Validate color code
        if (string.IsNullOrWhiteSpace(colorCode) || !colorCode.StartsWith("#") || colorCode.Length != 7)
        {
            throw new InvalidOperationException("Mã màu không hợp lệ.");
        }

        eventType.EventTypeName = name;
        eventType.Description = description;
        eventType.IconClass = iconClass;
        eventType.ColorCode = colorCode;
        eventType.IsActive = isActive;

        await _context.SaveChangesAsync();
        return eventType;
    }

    public async Task<bool> DeleteEventTypeAsync(int id)
    {
        var eventType = await _context.EventTypes
            .Include(et => et.Events)
            .FirstOrDefaultAsync(et => et.EventTypeId == id);

        if (eventType == null)
        {
            throw new InvalidOperationException("Không tìm thấy loại sự kiện.");
        }

        if (eventType.Events.Any())
        {
            throw new InvalidOperationException("Không thể xóa loại sự kiện đã có sự kiện.");
        }

        _context.EventTypes.Remove(eventType);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<EventType> UpdateEventTypeStatusAsync(int id, bool isActive)
    {
        var eventType = await _context.EventTypes.FindAsync(id);
        if (eventType == null)
        {
            throw new InvalidOperationException("Không tìm thấy loại sự kiện.");
        }

        eventType.IsActive = isActive;
        await _context.SaveChangesAsync();
        return eventType;
    }

    private async Task<string?> SaveBannerImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "events");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/events/{uniqueFileName}";
    }

    public async Task<bool> IsOrganizerAsync(int eventId, int userId)
    {
        var evt = await _context.Events.FindAsync(eventId);
        return evt?.OrganizerId == userId;
    }

    public async Task<bool> CanRegisterAsync(int eventId)
    {
        var evt = await _context.Events.FindAsync(eventId);
        if (evt == null || evt.IsDeleted)
        {
            return false;
        }

        // Check if event is upcoming and registration deadline hasn't passed
        var now = DateTime.UtcNow;
        if (evt.StartDate <= now || evt.Status != "Upcoming")
        {
            return false;
        }

        if (evt.RegistrationDeadline.HasValue && evt.RegistrationDeadline.Value <= now)
        {
            return false;
        }

        // Check if event has reached max attendees
        if (evt.MaxAttendees.HasValue)
        {
            var currentRegistrations = await _context.Registrations
                .CountAsync(r => r.EventId == eventId && !r.IsDeleted);
            if (currentRegistrations >= evt.MaxAttendees.Value)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<bool> HasRegisteredAsync(int eventId, int userId)
    {
        return await _context.Registrations
            .AnyAsync(r => r.EventId == eventId && r.AttendeeId == userId && !r.IsDeleted);
    }

    public async Task<List<Event>> SearchEventsAsync(
        string? searchTerm = null,
        int? eventTypeId = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        string? sortOrder = null,
        bool includeDeleted = false)
    {
        var query = _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(e => !e.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(e => 
                e.EventName.ToLower().Contains(searchTerm) ||
                e.Description.ToLower().Contains(searchTerm) ||
                e.Location.ToLower().Contains(searchTerm) ||
                (e.Tags != null && e.Tags.ToLower().Contains(searchTerm))
            );
        }

        if (eventTypeId.HasValue)
        {
            query = query.Where(e => e.EventTypeId == eventTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.StartDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.EndDate <= endDate.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(e => e.Price > minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(e => e.Price <= maxPrice.Value);
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            var isAscending = string.IsNullOrWhiteSpace(sortOrder) || sortOrder.ToLower() == "asc";

            query = sortBy.ToLower() switch
            {
                "name" => isAscending 
                    ? query.OrderBy(e => e.EventName) 
                    : query.OrderByDescending(e => e.EventName),
                
                "price" => isAscending 
                    ? query.OrderBy(e => e.Price) 
                    : query.OrderByDescending(e => e.Price),
                
                "registrations" => isAscending 
                    ? query.OrderBy(e => e.Registrations.Count) 
                    : query.OrderByDescending(e => e.Registrations.Count),
                
                _ => isAscending 
                    ? query.OrderBy(e => e.StartDate) 
                    : query.OrderByDescending(e => e.StartDate)
            };
        }
        else
        {
            query = query.OrderByDescending(e => e.CreatedAt);
        }

        return await query.ToListAsync();
    }

    public async Task SendEventRemindersAsync()
    {
        var now = DateTime.UtcNow;
        var tomorrow = now.AddDays(1);
        var nextWeek = now.AddDays(7);

        // Get upcoming events
        var upcomingEvents = await _context.Events
            .Include(e => e.Registrations)
                .ThenInclude(r => r.Attendee)
            .Where(e => 
                !e.IsDeleted &&
                e.Status == "Upcoming" &&
                e.StartDate > now &&
                e.StartDate <= nextWeek)
            .ToListAsync();

        foreach (var evt in upcomingEvents)
        {
            var registrations = evt.Registrations
                .Where(r => !r.IsDeleted && r.Status == "Registered")
                .ToList();

            foreach (var registration in registrations)
            {
                var attendee = registration.Attendee;
                var daysUntilEvent = (evt.StartDate - now).Days;

                // Send reminder 1 day before
                if (evt.StartDate.Date == tomorrow.Date)
                {
                    var emailBody = $@"<p>Chào {attendee.FullName},</p>
<p>Nhắc nhở: Sự kiện <strong>{evt.EventName}</strong> sẽ diễn ra vào ngày mai.</p>
<p>Thông tin sự kiện:</p>
<ul>
    <li>Thời gian: {evt.StartDate:dd/MM/yyyy HH:mm}</li>
    <li>Địa điểm: {evt.Location}</li>
    <li>Địa chỉ: {evt.Address}</li>
</ul>
<p>Vui lòng đến đúng giờ và mang theo QR code để check-in.</p>
<p>Trân trọng,<br>Ban tổ chức</p>";

                    await _emailService.SendEmailAsync(
                        attendee.Email,
                        $"Nhắc nhở: Sự kiện {evt.EventName} diễn ra vào ngày mai",
                        emailBody,
                        attendee.FullName);
                }
                // Send reminder 1 week before
                else if (evt.StartDate.Date == nextWeek.Date)
                {
                    var emailBody = $@"<p>Chào {attendee.FullName},</p>
<p>Nhắc nhở: Sự kiện <strong>{evt.EventName}</strong> sẽ diễn ra sau 1 tuần nữa.</p>
<p>Thông tin sự kiện:</p>
<ul>
    <li>Thời gian: {evt.StartDate:dd/MM/yyyy HH:mm}</li>
    <li>Địa điểm: {evt.Location}</li>
    <li>Địa chỉ: {evt.Address}</li>
</ul>
<p>Nếu bạn không thể tham gia, vui lòng hủy đăng ký trước ngày diễn ra sự kiện.</p>
<p>Trân trọng,<br>Ban tổ chức</p>";

                    await _emailService.SendEmailAsync(
                        attendee.Email,
                        $"Nhắc nhở: Sự kiện {evt.EventName} diễn ra sau 1 tuần",
                        emailBody,
                        attendee.FullName);
                }
            }
        }
    }
} 