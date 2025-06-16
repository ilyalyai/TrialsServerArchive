using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TrialsServerArchive.Models;
using TrialsServerArchive.Models.Objects;

namespace TrialsServerArchive.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        // DbSet для ваших сущностей
        public DbSet<BaseObject> Objects { get; set; }
        public DbSet<Tooling> Toolings { get; set; }
        public DbSet<TrialTooling> TrialToolings { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) ||
                        property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                            v => v
                        ));
                    }
                }
            }

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
    }
}