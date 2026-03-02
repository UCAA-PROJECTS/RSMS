using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RSMS.Models;

namespace RSMS.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Shelter> Shelters { get; set; }
        public DbSet<SensorReading> Readings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Shelter>().
                HasKey(s => new { s.ShelterCode });

            builder.Entity<SensorReading>()
                .HasOne(s => s.Shelter)
                .WithMany(r => r.SensorReadings)
                .HasForeignKey(p => p.ShelterCode);

            builder.Entity<Shelter>().HasData(
                new Shelter { ShelterCode = "GP001", ShelterName = "GP Shelter" },
                new Shelter { ShelterCode = "ILS002", ShelterName = "ILS Shelter" },
                new Shelter { ShelterCode = "DVOR003", ShelterName = "DVOR Shelter" });
                
        }
    }
    
}
