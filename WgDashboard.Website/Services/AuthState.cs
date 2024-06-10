using WgDashboard.Website.Models;

namespace WgDashboard.Website.Services
{
    public static class AuthState
    {
        public static int Id { get; private set; } = 0;
        public static string Username { get; private set; } = "";
        public static string Role { get; private set; } = UserRoles.Anonymous;
        public static string? Name { get; private set; }

        public static void SetAuthenticatedUser(int id, string username, string role, string? name)
        {
            Id = id;
            Username = username;
            Role = role;
            Name = name;
        }

        public static void LogoutUser()
        {
            Id = 0;
            Username = "";
            Role = UserRoles.Anonymous;
            Name = null;
        }
    }
}
