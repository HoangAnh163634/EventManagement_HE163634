using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int RegistrationId { get; set; }

    public int? Rating { get; set; }

    public string? Comments { get; set; }

    public DateTime SubmittedAt { get; set; }

    public virtual Registration Registration { get; set; } = null!;
}
