using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public int? EventId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;

    public string Status { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime SentAt { get; set; }

    public int? SentBy { get; set; }

    public string? Link { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int RetryCount { get; set; }

    public string? ErrorMessage { get; set; }

    public virtual Event? Event { get; set; }

    public virtual User? SentByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
