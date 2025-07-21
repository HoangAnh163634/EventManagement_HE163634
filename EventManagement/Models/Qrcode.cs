using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class Qrcode
{
    public int QrcodeId { get; set; }

    public int RegistrationId { get; set; }

    public int EventId { get; set; }

    public string QrcodeValue { get; set; } = null!;

    public string? QrcodeImageUrl { get; set; }

    public DateTime GeneratedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public int? UsedBy { get; set; }

    public bool IsActive { get; set; }

    public int ScanCount { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual Registration Registration { get; set; } = null!;

    public virtual User? UsedByNavigation { get; set; }
}
