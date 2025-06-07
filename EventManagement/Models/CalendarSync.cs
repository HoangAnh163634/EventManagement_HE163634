using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class CalendarSync
{
    public int SyncId { get; set; }

    public int UserId { get; set; }

    public int EventId { get; set; }

    public string? GoogleCalendarEventId { get; set; }

    public DateTime LastSyncedAt { get; set; }

    public string SyncStatus { get; set; } = null!;

    public string? ErrorMessage { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
