using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WgDashboardApi.Models
{
    /// <summary>
    /// The profile that matches the database and possibly contains sensitive information that should not be serialized and sent to the client
    /// </summary>
    public class User 
    {
        [Key]
        public int Id { get; set; } = 0;
        [Required]
        public string Role { get; set; } = UserRoles.Anonymous;
        [Required]
        public string Username { get; set; } = "";
        [Required]
        [JsonIgnore] // just in case
        public string Password { get; set; } = "";
        public string? Name { get; set; }
    }

    /// <summary>
    /// Represents all valid roles and includes a function to check if a role is valid
    /// </summary>
    public static class UserRoles
    {
        public static readonly string Anonymous = "anonymous";
        public static readonly string User = "user";
        public static readonly string Admin = "admin";

        public static bool IsValidRole(string? role) => (role == Anonymous) || (role == User) || (role == Admin);
    }
}
