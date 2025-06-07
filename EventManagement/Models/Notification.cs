using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public int? EventId { get; set; }

    public string NotificationType { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;

    public string Status { get; set; } = null!;

    public virtual Event? Event { get; set; }

    public virtual User User { get; set; } = null!;
}
