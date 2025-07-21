using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class SocialShare
{
    public int ShareId { get; set; }

    public int EventId { get; set; }

    public int? UserId { get; set; }

    public string Platform { get; set; } = null!;

    public string? SharedUrl { get; set; }

    public string? ShareStatus { get; set; }

    public string? ShareText { get; set; }

    public DateTime SharedAt { get; set; }

    public string? Ipaddress { get; set; }

    public string? UserAgent { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual User? User { get; set; }
}
