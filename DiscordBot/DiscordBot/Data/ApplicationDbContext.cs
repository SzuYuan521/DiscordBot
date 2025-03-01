using Microsoft.EntityFrameworkCore;
using DiscordBot.Models;

namespace DiscordBot.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // 註冊資料表
        public DbSet<JobSchedule> JobSchedules { get; set; }
        public DbSet<DiscordMessage> DiscordMessages { get; set; } // 用於發送訊息
        public DbSet<MonitoredMessage> MonitoredMessages { get; set; } // 用於監聽 Discord 訊息
        public DbSet<DiscordRole> DiscordRoles { get; set; }
        public DbSet<DiscordChannel> DiscordChannels { get; set; }
        public DbSet<RoleMagicPact> RoleMagicPacts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 設定 JobSchedule 預設值
            modelBuilder.Entity<JobSchedule>()
                .Property(j => j.CreatedAt)
                .HasDefaultValueSql("NOW()");

            modelBuilder.Entity<JobSchedule>()
                .Property(j => j.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            // 設定 DiscordMessage
            modelBuilder.Entity<DiscordMessage>(entity =>
            {
                entity.HasKey(m => m.Id); // 主鍵
                entity.Property(m => m.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // 設定 MonitoredMessage (用於監聽 Discord 訊息)
            modelBuilder.Entity<MonitoredMessage>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => m.MessageId).IsUnique();
            });

            // RoleMagicPact 與 DiscordChannel 關聯
            modelBuilder.Entity<RoleMagicPact>()
                .HasOne(p => p.DiscordChannel)
                .WithMany()
                .HasForeignKey(p => p.DiscordChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            // RoleMagicPact 與 MonitoredMessage 關聯
            modelBuilder.Entity<RoleMagicPact>()
                .HasOne(p => p.MonitoredMessage)
                .WithMany()
                .HasForeignKey(p => p.MonitoredMessageId)
                .HasPrincipalKey(m => m.MessageId) // 指定 MonitoredMessage.MessageId 為外鍵
                .OnDelete(DeleteBehavior.Cascade);

            // RoleMagicPact 與 DiscordRole 關聯
            modelBuilder.Entity<RoleMagicPact>()
                .HasOne(p => p.DiscordRole)
                .WithMany()
                .HasForeignKey(p => p.DiscordRoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
