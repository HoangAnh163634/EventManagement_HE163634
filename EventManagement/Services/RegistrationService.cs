using EventManagement.Models;
using EventManagement.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace EventManagement.Services;

public class RegistrationService
{
    private readonly EventManagementDbContext _context;
    private readonly EmailService _emailService;
    private readonly IWebHostEnvironment _environment;

    public RegistrationService(
        EventManagementDbContext context,
        EmailService emailService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _emailService = emailService;
        _environment = environment;
    }

    public async Task<List<Registration>> GetUserRegistrationsAsync(int userId)
    {
        return await _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.Qrcode)
            .Include(r => r.Feedback)
            .Where(r => r.AttendeeId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.RegistrationDate)
            .ToListAsync();
    }

    public async Task<Registration?> GetRegistrationByIdAsync(int id)
    {
        return await _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.Attendee)
            .Include(r => r.Qrcode)
            .Include(r => r.Feedback)
            .FirstOrDefaultAsync(r => r.RegistrationId == id && !r.IsDeleted);
    }

    public async Task<Registration?> GetRegistrationByIdAsync(int id, bool includeDeleted)
    {
        var query = _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.Attendee)
            .Include(r => r.Qrcode)
            .Include(r => r.Feedback)
            .AsQueryable();
        if (!includeDeleted)
            query = query.Where(r => !r.IsDeleted);
        return await query.FirstOrDefaultAsync(r => r.RegistrationId == id);
    }

    public async Task<Registration> RegisterForEventAsync(int eventId, int userId, string? specialRequests = null)
    {
        var evt = await _context.Events
            .Include(e => e.Organizer)
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (evt == null)
        {
            throw new InvalidOperationException("Sự kiện không tồn tại.");
        }

        if (evt.IsDeleted)
        {
            throw new InvalidOperationException("Sự kiện đã bị xóa.");
        }

        if (evt.Status != "Upcoming")
        {
            throw new InvalidOperationException("Sự kiện không còn nhận đăng ký.");
        }

        if (evt.RegistrationDeadline.HasValue && evt.RegistrationDeadline.Value <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Đã hết hạn đăng ký.");
        }

        if (evt.MaxAttendees.HasValue)
        {
            var currentRegistrations = await _context.Registrations
                .CountAsync(r => r.EventId == eventId && !r.IsDeleted);
            if (currentRegistrations >= evt.MaxAttendees.Value)
            {
                throw new InvalidOperationException("Sự kiện đã đủ số lượng người tham gia.");
            }
        }

        // Kiểm tra đã đăng ký sự kiện này chưa
        var existingRegistration = await _context.Registrations
            .AnyAsync(r => r.EventId == eventId && r.AttendeeId == userId && !r.IsDeleted);
        if (existingRegistration)
        {
            throw new InvalidOperationException("Bạn đã đăng ký tham gia sự kiện này.");
        }

        // Kiểm tra trùng lịch với các sự kiện đã đăng ký
        var userRegistrations = await _context.Registrations
            .Include(r => r.Event)
            .Where(r => r.AttendeeId == userId && !r.IsDeleted && r.Status != "Cancelled")
            .ToListAsync();

        foreach (var reg in userRegistrations)
        {
            if (evt.StartDate < reg.Event.EndDate && evt.EndDate > reg.Event.StartDate)
            {
                throw new InvalidOperationException($"Thời gian sự kiện trùng với sự kiện '{reg.Event.EventName}' mà bạn đã đăng ký.");
            }
        }

        var registration = new Registration
        {
            EventId = eventId,
            AttendeeId = userId,
            RegistrationDate = DateTime.UtcNow,
            Status = "Registered",
            SpecialRequests = specialRequests,
            CreatedAt = DateTime.UtcNow
        };

        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Generate QR code
        var qrCode = await GenerateQrCodeAsync(registration);
        registration.Qrcode = qrCode;
        await _context.SaveChangesAsync();

        // Send confirmation email
        var attendee = await _context.Users.FindAsync(userId);
        if (attendee != null)
        {
            var emailBody = $@"<p>Chào {attendee.FullName},</p>
<p>Cảm ơn bạn đã đăng ký tham gia sự kiện <strong>{evt.EventName}</strong>.</p>
<p>Thông tin sự kiện:</p>
<ul>
    <li>Thời gian: {evt.StartDate:dd/MM/yyyy HH:mm} - {evt.EndDate:dd/MM/yyyy HH:mm}</li>
    <li>Địa điểm: {evt.Location}</li>
    <li>Người tổ chức: {evt.Organizer.FullName}</li>
</ul>
<p>Vui lòng giữ email này để check-in khi tham gia sự kiện.</p>
<p>Nếu bạn không thể tham gia, vui lòng hủy đăng ký trước ngày diễn ra sự kiện.</p>
<p>Trân trọng,<br>Ban tổ chức</p>";

            await _emailService.SendEmailAsync(
                attendee.Email,
                $"Xác nhận đăng ký sự kiện: {evt.EventName}",
                emailBody,
                attendee.FullName);
        }

        return registration;
    }

    public async Task<Registration> CancelRegistrationAsync(int registrationId, int userId, string? reason = null)
    {
        var registration = await _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.Attendee)
            .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

        if (registration == null)
        {
            throw new InvalidOperationException("Không tìm thấy đăng ký.");
        }

        if (registration.AttendeeId != userId)
        {
            throw new InvalidOperationException("Bạn không có quyền hủy đăng ký này.");
        }

        if (registration.Status == "Cancelled")
        {
            throw new InvalidOperationException("Đăng ký đã bị hủy.");
        }

        if (registration.Event.StartDate <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Không thể hủy đăng ký sau khi sự kiện đã bắt đầu.");
        }

        registration.Status = "Cancelled";
        registration.CancellationReason = reason;
        registration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Send cancellation email
        var emailBody = $@"<p>Chào {registration.Attendee.FullName},</p>
<p>Bạn đã hủy đăng ký tham gia sự kiện <strong>{registration.Event.EventName}</strong>.</p>
<p>Lý do hủy: {reason ?? "Không có"}</p>
<p>Nếu bạn muốn đăng ký lại, vui lòng truy cập trang sự kiện.</p>
<p>Trân trọng,<br>Ban tổ chức</p>";

        await _emailService.SendEmailAsync(
            registration.Attendee.Email,
            $"Xác nhận hủy đăng ký: {registration.Event.EventName}",
            emailBody,
            registration.Attendee.FullName);

        return registration;
    }

    public async Task<Registration> CheckInAsync(int registrationId, int userId, string method, string location)
    {
        var registration = await _context.Registrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

        if (registration == null)
        {
            throw new InvalidOperationException("Không tìm thấy đăng ký.");
        }

        if (registration.Status != "Registered")
        {
            throw new InvalidOperationException("Không thể check-in với trạng thái hiện tại.");
        }

        if (registration.Event.StartDate > DateTime.UtcNow)
        {
            throw new InvalidOperationException("Chưa đến thời gian check-in.");
        }

        if (registration.Event.EndDate < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Sự kiện đã kết thúc.");
        }

        registration.Status = "CheckedIn";
        registration.CheckInTime = DateTime.UtcNow;
        registration.CheckInMethod = method;
        registration.CheckInLocation = location;
        registration.CheckInBy = userId;
        registration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return registration;
    }

    public async Task<List<Registration>> GetAllRegistrationsAsync()
    {
        return await _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.Attendee)
            .Where(r => !r.IsDeleted)
            .ToListAsync();
    }

    public async Task UpdateRegistrationAsync(Registration reg)
    {
        _context.Registrations.Update(reg);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Registration>> GetDeletedRegistrationsAsync()
    {
        return await _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.Attendee)
            .Where(r => r.IsDeleted)
            .ToListAsync();
    }

    public async Task DeleteRegistrationPermanentlyAsync(int id)
    {
        var reg = await _context.Registrations.FindAsync(id);
        if (reg != null)
        {
            _context.Registrations.Remove(reg);
            await _context.SaveChangesAsync();
        }
    }

    private async Task<Qrcode> GenerateQrCodeAsync(Registration registration)
    {
        var qrCodeValue = $"REG_{registration.RegistrationId}_{Guid.NewGuid():N}";
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrCodeValue, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new BitmapByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "qrcodes");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = $"qr_{registration.RegistrationId}_{DateTime.UtcNow:yyyyMMddHHmmss}.png";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await File.WriteAllBytesAsync(filePath, qrCodeImage);

        return new Qrcode
        {
            RegistrationId = registration.RegistrationId,
            EventId = registration.EventId,
            QrcodeValue = qrCodeValue,
            QrcodeImageUrl = $"/uploads/qrcodes/{fileName}",
            GeneratedAt = DateTime.UtcNow,
            ExpiresAt = registration.Event.EndDate,
            IsActive = true
        };
    }

    public async Task<bool> ValidateQrCodeAsync(string qrCodeValue)
    {
        var qrCode = await _context.Qrcodes
            .Include(q => q.Registration)
            .FirstOrDefaultAsync(q => q.QrcodeValue == qrCodeValue);

        if (qrCode == null || !qrCode.IsActive)
        {
            return false;
        }

        if (qrCode.ExpiresAt.HasValue && qrCode.ExpiresAt.Value < DateTime.UtcNow)
        {
            return false;
        }

        if (qrCode.Registration.Status != "Registered")
        {
            return false;
        }

        return true;
    }
} 