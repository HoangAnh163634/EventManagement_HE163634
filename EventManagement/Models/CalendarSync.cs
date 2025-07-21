using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class CalendarSync
{
    public int SyncId { get; set; }

    public int UserId { get; set; }

    public int EventId { get; set; }

    public string Provider { get; set; } = null!;

    public string? ExternalCalendarId { get; set; }

    public string? ExternalEventId { get; set; }

    public string? SyncToken { get; set; }

    public DateTime LastSyncedAt { get; set; }

    public DateTime? NextSyncAt { get; set; }

    public string SyncStatus { get; set; } = null!;

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public bool IsActive { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
