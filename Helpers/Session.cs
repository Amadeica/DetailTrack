using DetailTrack.Models;

namespace DetailTrack.Helpers
{
    public static class Session
    {
        public static User? CurrentUser { get; set; }

        public static void Clear() => CurrentUser = null;

        public static bool IsAuthenticated => CurrentUser != null;
    }
}
