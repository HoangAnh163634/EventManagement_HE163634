using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class Qrcode
{
    public int QrcodeId { get; set; }

    public int RegistrationId { get; set; }

    public string QrcodeValue { get; set; } = null!;

    public DateTime GeneratedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual Registration Registration { get; set; } = null!;
}
