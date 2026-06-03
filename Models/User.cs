using System;
using System.Collections.Generic;

namespace DetailTrack.Models;

public partial class User
{
    public int Id { get; set; }

    public string Login { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public int RoleId { get; set; }

    public int WorkshopId { get; set; }

    public int SpecializationId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Request> RequestConstructors { get; set; } = new List<Request>();

    public virtual ICollection<RequestFile> RequestFiles { get; set; } = new List<RequestFile>();

    public virtual ICollection<RequestHistory> RequestHistories { get; set; } = new List<RequestHistory>();

    public virtual ICollection<Request> RequestProgrammers { get; set; } = new List<Request>();

    public virtual ICollection<Request> RequestSetupTechnicians { get; set; } = new List<Request>();

    public virtual ICollection<Request> RequestTechnologists { get; set; } = new List<Request>();

    public virtual Role Role { get; set; } = null!;

    public virtual Specialization Specialization { get; set; } = null!;

    public virtual ICollection<ToolRequest> ToolRequestAssignedEngineers { get; set; } = new List<ToolRequest>();

    public virtual ICollection<ToolRequest> ToolRequestRequestedBies { get; set; } = new List<ToolRequest>();

    public virtual Workshop Workshop { get; set; } = null!;
}
