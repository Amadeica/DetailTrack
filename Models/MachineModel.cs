using System;
using System.Collections.Generic;

namespace DetailTrack.Models;

public partial class MachineModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int MachineTypeId { get; set; }

    public bool IsRemoved { get; set; }

    public virtual MachineType MachineType { get; set; } = null!;

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
