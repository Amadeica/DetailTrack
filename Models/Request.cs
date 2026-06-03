using System;
using System.Collections.Generic;

namespace DetailTrack.Models;

public partial class Request
{
    public int Id { get; set; }

    public string RequestNumber { get; set; } = null!;

    public string DetailName { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int ConstructorId { get; set; }

    public int? TechnologistId { get; set; }

    public int? ProgrammerId { get; set; }

    public int? SetupTechnicianId { get; set; }

    public int? MachineTypeId { get; set; }

    public int? MachineModelId { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? TechProcessUploadedAt { get; set; }

    public DateTime? ProgramUploadedAt { get; set; }

    public DateTime? ImplementationDate { get; set; }

    public bool IsCompleted { get; set; }

    public int? WorkshopId { get; set; }

    public bool IsRemoved { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual User Constructor { get; set; } = null!;

    public virtual MachineModel? MachineModel { get; set; }

    public virtual MachineType? MachineType { get; set; }

    public virtual User? Programmer { get; set; }

    public virtual ICollection<RequestFile> RequestFiles { get; set; } = new List<RequestFile>();

    public virtual ICollection<RequestHistory> RequestHistories { get; set; } = new List<RequestHistory>();

    public virtual User? SetupTechnician { get; set; }

    public virtual User? Technologist { get; set; }

    public virtual ICollection<ToolRequest> ToolRequests { get; set; } = new List<ToolRequest>();

    public virtual Workshop? Workshop { get; set; }
}
