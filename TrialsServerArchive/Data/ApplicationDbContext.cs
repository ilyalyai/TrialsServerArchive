using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrialsServerArchive.Models;
using TrialsServerArchive.Models.Objects;

namespace TrialsServerArchive.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<BaseObject> Objects { get; set; }
        public DbSet<Tooling> Toolings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка TPH для иерархии объектов
            modelBuilder.Entity<BaseObject>()
                .HasDiscriminator<string>("ObjectType")
                .HasValue<Sample>("Sample")
                .HasValue<TrialObject>("TrialObject")
                .HasValue<ObjectInJournal>("ObjectInJournal");

            // Настройка связи многие-ко-многим
            modelBuilder.Entity<TrialTooling>()
                .HasKey(tt => new { tt.TrialObjectId, tt.ToolingId });

            modelBuilder.Entity<TrialTooling>()
                .HasOne(tt => tt.TrialObject)
                .WithMany(t => t.ToolingLinks)
                .HasForeignKey(tt => tt.TrialObjectId);

            modelBuilder.Entity<TrialTooling>()
                .HasOne(tt => tt.Tooling)
                .WithMany(t => t.TrialLinks)
                .HasForeignKey(tt => tt.ToolingId);
        }

        public DbSet<TrialTooling> TrialToolings { get; set; }
    }
}
