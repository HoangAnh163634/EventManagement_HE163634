using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsEmailVerified { get; set; } = false;

    public string? EmailVerificationToken { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetExpires { get; set; }

    public string? ProfileImageUrl { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<CalendarSync> CalendarSyncs { get; set; } = new List<CalendarSync>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Notification> NotificationSentByNavigations { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationUsers { get; set; } = new List<Notification>();

    public virtual ICollection<Qrcode> Qrcodes { get; set; } = new List<Qrcode>();

    public virtual ICollection<Registration> RegistrationAttendees { get; set; } = new List<Registration>();

    public virtual ICollection<Registration> RegistrationCheckInByNavigations { get; set; } = new List<Registration>();

    public virtual ICollection<SocialShare> SocialShares { get; set; } = new List<SocialShare>();

    public virtual ICollection<UserRole> UserRoleAssignedByNavigations { get; set; } = new List<UserRole>();

    public virtual ICollection<UserRole> UserRoleUsers { get; set; } = new List<UserRole>();
}
