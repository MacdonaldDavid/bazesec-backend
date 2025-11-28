using Microsoft.EntityFrameworkCore;
using BazeSec.Models;

namespace BazeSec.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Personnel> Personnel { get; set; }

        // NEW TABLES
        public DbSet<KeyItem> KeyItems { get; set; }
        public DbSet<KeyLog> KeyLogs { get; set; }

        // VISITOR MODULE
        public DbSet<Visitor> Visitors { get; set; }

        // ANONYMOUSTIP MODULE
        public DbSet<AnonymousTip> AnonymousTips { get; set; }
        // 🚨 EMERGENCY ALERT MODULE
        public DbSet<EmergencyAlert> EmergencyAlerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<KeyLog>()
                .HasOne(k => k.KeyItem)
                .WithMany()
                .HasForeignKey(k => k.KeyItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
