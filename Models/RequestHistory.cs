using System;
using System.Collections.Generic;

namespace DetailTrack.Models;

public partial class RequestHistory
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public string? Comment { get; set; }

    public int ChangedById { get; set; }

    public DateTime? ChangedAt { get; set; }

    public virtual User ChangedBy { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;
}
