﻿using System.ComponentModel.DataAnnotations;

namespace WgDashboardApi.Models
{
    public class UpdateUserRequest
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public string? Name { get; set; }

    }
}
