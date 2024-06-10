using Microsoft.EntityFrameworkCore;
using WgDashboard.Api.Models;

namespace WgDashboard.Api.Data
{
    public class WireguardDbContext : DbContext
    {
        public WireguardDbContext(DbContextOptions<WireguardDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Peer> Peers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("User", "dbo");
            modelBuilder.Entity<Peer>().ToTable("Peer", "dbo");
        }
    }
}
