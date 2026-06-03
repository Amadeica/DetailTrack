using System;
using System.Collections.Generic;

namespace DetailTrack.Models;

public partial class ToolRequest
{
    public int Id { get; set; }

    public int? MainRequestId { get; set; }

    public string ToolName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int RequestedById { get; set; }

    public int? AssignedEngineerId { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual User? AssignedEngineer { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual Request? MainRequest { get; set; }

    public virtual ICollection<RequestFile> RequestFiles { get; set; } = new List<RequestFile>();

    public virtual User RequestedBy { get; set; } = null!;
}
