using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class Event
{
    public int EventId { get; set; }

    public int OrganizerId { get; set; }

    public int EventTypeId { get; set; }

    public string EventName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Location { get; set; } = null!;

    public string? Address { get; set; }

    public bool IsPublic { get; set; }

    public string PrivacyLevel { get; set; } = null!;

    public int? MaxAttendees { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? RegistrationDeadline { get; set; }

    public decimal? Price { get; set; }

    public string? Currency { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime LastModified { get; set; }

    public string? FeedbackSummary { get; set; }

    public decimal? AverageRating { get; set; }

    public int TotalFeedbacks { get; set; }

    public string? BannerImageUrl { get; set; }

    public string? Tags { get; set; }

    public virtual ICollection<CalendarSync> CalendarSyncs { get; set; } = new List<CalendarSync>();

    public virtual EventType EventType { get; set; } = null!;

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual User Organizer { get; set; } = null!;

    public virtual ICollection<Qrcode> Qrcodes { get; set; } = new List<Qrcode>();

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    public virtual ICollection<SocialShare> SocialShares { get; set; } = new List<SocialShare>();
}
