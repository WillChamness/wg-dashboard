using System.ComponentModel.DataAnnotations;

namespace WgDashboard.Api.Models
{
    public class UpdatePeerRequest
    {
        [Required]
        public int Id { get; set; } = 0;
        [Required]
        public string PublicKey { get; set; } = "";
        [Required]
        public string AllowedIPs { get; set; } = "";
        [Required]
        public int OwnerId { get; set; } = 0;
        public string? DeviceType { get; set; }
        public string? DeviceDescription { get; set; }
    }
}
