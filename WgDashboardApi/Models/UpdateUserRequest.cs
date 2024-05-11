using System.ComponentModel.DataAnnotations;

namespace WgDashboardApi.Models
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
