namespace WgDashboard.Api.Models
{
    public class PeerProfile
    {
        public int Id { get; set; } = 0;
        public string PublicKey { get; set; } = "";
        public string AllowedIPs { get; set; } = "";
        public string? DeviceDescription { get; set; }
        public string? OwnerName { get; set; }
        public string OwnerUsername { get; set; } = "";
        public string? DeviceType { get; set; }
    }
}
