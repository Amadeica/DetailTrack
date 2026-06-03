using DetailTrack.Models;

namespace DetailTrack.Helpers;

public static class RequestQueryExtensions
{
    public static IQueryable<Request> ActiveOnly(this IQueryable<Request> query) =>
        query.Where(r => !r.IsRemoved);

    public static IQueryable<Request> RemovedOnly(this IQueryable<Request> query) =>
        query.Where(r => r.IsRemoved);
}
