using System.ComponentModel.DataAnnotations;

namespace WgDashboardApi.Models
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
}
