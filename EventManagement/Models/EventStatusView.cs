using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class EventStatusView
{
    public int EventId { get; set; }

    public string EventName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Location { get; set; } = null!;

    public bool IsPublic { get; set; }

    public string PrivacyLevel { get; set; } = null!;

    public int? MaxAttendees { get; set; }

    public decimal? Price { get; set; }

    public string? Currency { get; set; }

    public string ComputedStatus { get; set; } = null!;

    public string SetStatus { get; set; } = null!;

    public int OrganizerId { get; set; }

    public int EventTypeId { get; set; }

    public string EventTypeName { get; set; } = null!;

    public string OrganizerName { get; set; } = null!;

    public decimal? AverageRating { get; set; }

    public int TotalFeedbacks { get; set; }

    public int? TotalRegistrations { get; set; }

    public int? TotalCheckedIn { get; set; }

    public double? CheckInRate { get; set; }
}
