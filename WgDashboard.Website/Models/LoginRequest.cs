using System.ComponentModel.DataAnnotations;

namespace WgDashboard.Website.Models
{
    /// <summary>
    /// Represents the username/password that the user sends to the API
    /// </summary>
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = "";
        [Required]
        public string Password { get; set; } = "";
    }
}
