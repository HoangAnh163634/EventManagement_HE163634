using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int RegistrationId { get; set; }

    public int EventId { get; set; }

    public int AttendeeId { get; set; }

    public string? FeedbackType { get; set; }

    public int? Rating { get; set; }

    public string? Comments { get; set; }

    public string? Suggestions { get; set; }

    public bool? WouldRecommend { get; set; }

    public DateTime SubmittedAt { get; set; }

    public bool IsPublic { get; set; }

    public bool IsApproved { get; set; }

    public virtual User Attendee { get; set; } = null!;

    public virtual Event Event { get; set; } = null!;

    public virtual Registration Registration { get; set; } = null!;
}
