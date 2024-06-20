using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.IdentityModel.Protocols.Configuration;

namespace WgDashboard.Api.Data
{
    public class WireguardDbContextFactory : IDesignTimeDbContextFactory<WireguardDbContext>
    {
        public WireguardDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Migrations.json")
                .Build();

            var builder = new DbContextOptionsBuilder<WireguardDbContext>();
            var connectionString = config.GetConnectionString("WireguardDb");

            if (connectionString is null)
                throw new InvalidConfigurationException("ConnectionString not found within appsettings.Migrations.json");
            builder.UseSqlServer(connectionString);

            return new WireguardDbContext(builder.Options);
        }
    }
}
