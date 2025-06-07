using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class SocialShare
{
    public int ShareId { get; set; }

    public int EventId { get; set; }

    public int? UserId { get; set; }

    public string Platform { get; set; } = null!;

    public DateTime SharedAt { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual User? User { get; set; }
}
