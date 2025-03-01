using Microsoft.EntityFrameworkCore;
using DiscordBot.Models;

namespace DiscordBot.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // 註冊資料表
        public DbSet<JobSchedule> JobSchedules { get; set; }
        public DbSet<DiscordMessage> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobSchedule>()
                .Property(j => j.CreatedAt)
                .HasDefaultValueSql("NOW()");

            modelBuilder.Entity<JobSchedule>()
                .Property(j => j.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            modelBuilder.Entity<DiscordMessage>()
                .Property(m => m.CreatedAt)
                .HasDefaultValueSql("NOW()");
        }
    }
}
