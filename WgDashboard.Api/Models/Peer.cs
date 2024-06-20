using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace WgDashboard.Api.Models
{
    public class Peer
    {
        [Key]
        public int Id { get; set; } = 0;

        [Required]
        [MaxLength(75)]
        public string PublicKey { get; set; } = "";

        [Required]
        [MaxLength(19)]
        public string AllowedIPs { get; set; } = "";

        [MaxLength(100)]
        public string? DeviceDescription { get; set; }

        [Required]
        public int OwnerId { get; set; } = 0;

        [MaxLength(20)]
        public string? DeviceType { get; set; }

        [NotNull]
        public User? Owner { get; set; }
    }

    public static class DeviceTypes
    {
        public static readonly string Pc = "PC";
        public static readonly string Laptop = "Laptop";
        public static readonly string Mac = "Mac";
        public static readonly string Phone = "Phone";
        public static readonly string Other = "Other";

        public static bool IsValidDeviceType(string? type)
        {
            return type == null || type == Pc || type == Laptop || type == Mac || type == Phone || type == Other;
        }

        public static string[] AllValidTypes { get => new string[] { Pc, Laptop, Mac, Phone, Other }; }
    }
}
