using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class Registration
{
    public int RegistrationId { get; set; }

    public int EventId { get; set; }

    public int AttendeeId { get; set; }

    public DateTime RegistrationDate { get; set; }

    public DateTime? CheckInTime { get; set; }

    public string Status { get; set; } = null!;

    public virtual User Attendee { get; set; } = null!;

    public virtual Event Event { get; set; } = null!;

    public virtual Feedback? Feedback { get; set; }

    public virtual Qrcode? Qrcode { get; set; }
}
