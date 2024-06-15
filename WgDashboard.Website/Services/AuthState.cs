using WgDashboard.Website.Helpers;

namespace WgDashboard.Website.Services
{
    public static class AuthState
    {
        public static int Id { get; private set; } = 0;
        public static string Username { get; private set; } = "";
        public static string Role { get; private set; } = UserRoles.Anonymous;
        public static string? Name { get; private set; }
        private static long AuthExpiration { get; set; } = -1;
        public static bool Expired { get {
                DateTime currentTime = DateTime.Now;
                DateTimeOffset currentTimeOffset = new DateTimeOffset(currentTime);
                return currentTimeOffset.ToUnixTimeSeconds() >= AuthExpiration;
            } }

        public static void SetAuthenticatedUser(int id, string username, string role, string? name, long authExpiration)
        {
            Id = id;
            Username = username;
            Role = role;
            Name = name;
            AuthExpiration = authExpiration;
        }

        public static void LogoutUser()
        {
            Id = 0;
            Username = "";
            Role = UserRoles.Anonymous;
            Name = null;
            AuthExpiration = -1;
        }
    }
}
