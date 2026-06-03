using DetailTrack.Models;

namespace DetailTrack.Helpers;

public static class ReferenceQueryExtensions
{
    public static IQueryable<Workshop> ActiveOnly(this IQueryable<Workshop> query) =>
        query.Where(x => !x.IsRemoved);

    public static IQueryable<MachineType> ActiveOnly(this IQueryable<MachineType> query) =>
        query.Where(x => !x.IsRemoved);

    public static IQueryable<MachineModel> ActiveOnly(this IQueryable<MachineModel> query) =>
        query.Where(x => !x.IsRemoved);
}
