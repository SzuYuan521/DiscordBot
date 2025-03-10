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

        public DbSet<GuildMember> GuildMembers { get; set; } // 幫會會員
        public DbSet<OneLineBond> OneLineBonds { get; set; } // 一線牽
        public DbSet<MemberStatistics> MemberStatistics { get; set; } // 統計
        public DbSet<StatisticsConfig> StatisticsConfigs { get; set; } // 存放 Emoji 與統計類型對應關係


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
                entity.Property(m => m.MessageType)
                    .HasConversion<int>();
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

            // 設定 GuildMember
            modelBuilder.Entity<GuildMember>(entity =>
            {
                entity.HasKey(g => g.DiscordId);
                entity.Property(g => g.JoinDate).HasDefaultValueSql("NOW()");
            });

            // 設定 OneLineBond
            modelBuilder.Entity<OneLineBond>(entity =>
            {
                entity.HasKey(b => b.BondId);

                // 與 GuildMembers 的外鍵關聯
                entity.HasOne(b => b.Member)
                    .WithMany(g => g.OneLineBonds)
                    .HasForeignKey(b => b.DiscordId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 設定 MemberStatistics 表
            modelBuilder.Entity<MemberStatistics>(entity =>
            {
                entity.HasKey(m => m.DiscordId);

                // 設定與 GuildMember 的關聯
                entity.HasOne(m => m.GuildMember)
                      .WithOne(g => g.Statistics)
                      .HasForeignKey<MemberStatistics>(m => m.DiscordId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StatisticsConfig>()
                .HasIndex(s => s.Emoji)
                .IsUnique(); // 確保 Emoji 唯一
        }
    }
}
