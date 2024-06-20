using System.ComponentModel.DataAnnotations;

namespace WgDashboard.Api.Models
{
    public class NewPeerRequest
    {
        [Required]
        public string PublicKey { get; set; } = "";

        [Required]
        public string AllowedIPs { get; set; } = "";

        public string? DeviceDescription { get; set; }

        [Required]
        public int OwnerId { get; set; } = 0;

        public string? DeviceType { get; set; }
    }
}
