using System.ComponentModel.DataAnnotations;

namespace WgDashboardWebsite.Models
{
    public class UpdateUserRequest
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string? Username { get; set; }
        [Required]
        public string Role = UserRoles.Anonymous;
        public string? Name { get; set; }

    }
}
