﻿using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.Configuration;
using System.Text.Json;

namespace WgDashboard.Api.Helpers
{
    public interface ISecurityInitialSettings
    {
        public string InitialUsername { get; }
        public string InitialPassword { get; }
        public string InitialName { get; }
        public bool CreateAdmin { get; }
    }

    public sealed class SecurityInitialSettings : ISecurityInitialSettings
    {
        public string InitialUsername { get; private set; } = "";
        public string InitialPassword { get; private set; } = "";
        public string InitialName { get; private set; } = "";
        public bool CreateAdmin { get; private set; } = false;


        public SecurityInitialSettings(IConfiguration config, IWebHostEnvironment environment)
        {
            if(environment.IsDevelopment())
            {
                InitialUsername = "admin";
                InitialPassword = "admin";
                InitialName = "Development Admin";
                CreateAdmin = true;
                return;
            }

            string? username = config.GetSection("AdminCredentials").GetValue<string>("Username");
            string? password = config.GetSection("AdminCredentials").GetValue<string>("Password");
            bool? initialize = config.GetSection("AdminCredentials").GetValue<bool>("Initialize");

            if (initialize is null)
                throw new InvalidConfigurationException("AdminCredentials:Initialize is a required setting.");

            if (initialize == true && (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)))
                throw new InvalidConfigurationException("Initialize set to true, but username or password not found or is empty");

            if(initialize == true && environment.IsProduction())
            {
                SetInitializeToFalse();
            }

            // at this point, the only way for username and password to be null is if the environment is production and initialized is set to false
            // in that case, dont really care what the username and password are
            InitialUsername = username ?? "";
            InitialPassword = password ?? "";
            InitialName = "Administrator";
            CreateAdmin = initialize ?? false; // cant be null at this point, but need to check to satisfy type definition of CreateAdmin
        }

        private static void SetInitializeToFalse()
        {
            string appsettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")!; // validated not null by Program.cs, assuming that the program is running in production
            string json = File.ReadAllText(appsettingsPath);

            var config = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json)!;
            config["AdminCredentials"]["Initialize"] = false;
            config["AdminCredentials"]["Username"] = "";
            config["AdminCredentials"]["Password"] = "";

            var updatedConfigJson = JsonSerializer.Serialize(config, new JsonSerializerOptions() { WriteIndented = true });

            File.WriteAllText(appsettingsPath, updatedConfigJson);
        }
    }
}
