﻿using System.ComponentModel.DataAnnotations;

namespace WgDashboardWebsite.Models
{
    public class SignupRequest
    {
        [Required]
        public string? Username { get; set; } 
        [Required]
        public string? Password { get; set; } 
        public string? Name {  get; set; }
    }
}
