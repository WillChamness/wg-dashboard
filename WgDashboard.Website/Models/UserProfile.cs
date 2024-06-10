using System.ComponentModel.DataAnnotations;

namespace WgDashboard.Website.Models
{
    /// <summary>
    /// The profile that contains information that can be serialized and sent to the client.
    /// </summary>
    public class UserProfile
    {
        public int Id { get; set; } = 0;
        public string? Name { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = UserRoles.Anonymous;
    }

    public static class UserRoles
    {
        public static readonly string Anonymous = "anonymous";
        public static readonly string User = "user";
        public static readonly string Admin = "admin";

        public static bool IsValidRole(string? role) => (role == Anonymous) || (role == User) || (role == Admin);
    }
}
