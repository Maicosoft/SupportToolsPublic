using Microsoft.EntityFrameworkCore;
using SupportTools.Shared.Models;

namespace SupportTools.Server.Data {
    public class SupportToolsDbContext : DbContext {
        public SupportToolsDbContext(DbContextOptions<SupportToolsDbContext> options)
            : base(options) {
        }

        public DbSet<Apotheek> Apotheken {
            get; set;
        }
        public DbSet<ATCCode> ATCCodes {
            get; set;
        }
        public DbSet<ApotheekATC> ApotheekATCs {
            get; set;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            // Configure ApotheekATC as join entity
            modelBuilder.Entity<ApotheekATC>()
                .HasKey(x => x.Id); // Optional: use composite key if needed

            modelBuilder.Entity<ApotheekATC>()
                .HasOne(x => x.Apotheek)
                .WithMany(a => a.ApotheekATCs)
                .HasForeignKey(x => x.ApotheekId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApotheekATC>()
                .HasOne(x => x.ATCCode)
                .WithMany(c => c.ApotheekATCs)
                .HasForeignKey(x => x.ATCCodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
