using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class EventType
{
    public int EventTypeId { get; set; }

    public string EventTypeName { get; set; } = null!;

    public string? Description { get; set; }

    public string? IconClass { get; set; }

    public string? ColorCode { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
