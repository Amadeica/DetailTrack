using System;
using System.Collections.Generic;

namespace DetailTrack.Models;

public partial class Workshop
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsRemoved { get; set; }

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
