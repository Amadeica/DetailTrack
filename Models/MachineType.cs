using System;
using System.Collections.Generic;

namespace DetailTrack.Models;

public partial class MachineType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsRemoved { get; set; }

    public virtual ICollection<MachineModel> MachineModels { get; set; } = new List<MachineModel>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
