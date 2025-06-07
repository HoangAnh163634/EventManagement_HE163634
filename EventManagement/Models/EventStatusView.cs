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

    public int? MaxAttendees { get; set; }

    public string Status { get; set; } = null!;

    public int OrganizerId { get; set; }

    public int EventTypeId { get; set; }
}
