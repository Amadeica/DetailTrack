using System;
using System.Collections.Generic;

namespace DetailTrack.Models;

public partial class Comment
{
    public int Id { get; set; }

    public int? RequestId { get; set; }

    public int? ToolRequestId { get; set; }

    public int UserId { get; set; }

    public string Text { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Request? Request { get; set; }

    public virtual ToolRequest? ToolRequest { get; set; }

    public virtual User User { get; set; } = null!;
}
