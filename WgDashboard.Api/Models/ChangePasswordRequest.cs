using System.ComponentModel.DataAnnotations;

namespace WgDashboard.Api.Models
{
    /// <summary>
    /// Represents a user making a request to change their password
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required]
        public int Id { get; set; } // user's ID
        [Required]
        public string Password { get; set; } = "";
    }
}
