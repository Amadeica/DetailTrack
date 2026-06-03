using System;
using System.Collections.Generic;

namespace DetailTrack.Models;

public partial class RequestFile
{
    public int Id { get; set; }

    public int? RequestId { get; set; }

    public int? ToolRequestId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public int UploadedById { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual Request? Request { get; set; }

    public virtual ToolRequest? ToolRequest { get; set; }

    public virtual User UploadedBy { get; set; } = null!;
}
