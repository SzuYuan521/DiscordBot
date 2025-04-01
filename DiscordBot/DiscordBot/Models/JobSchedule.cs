using DiscordBot.Enums;
using DiscordBot.Extensions;
using System.ComponentModel.DataAnnotations;

namespace DiscordBot.Models
{
    public class JobSchedule
    {
        [Key]
        public int Id { get; set; }

        public JobType JobType { get; set; }  // 工作類型(不同類型的排程)
        public MessageType? MessageType { get; set; }  // 訊息類型(如果是發訊息)
        public int? MessageId { get; set; }  // 訊息ID(如果是發訊息)
        public string? Command { get; set; }  // 執行的指令
        public string CronExpression { get; set; }  // Quartz 排程表達式

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // 建立時間
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;  // 更新時間
        public bool Enabled { get; set; } = true;  // 是否啟用

        public JobSchedule(JobType jobType, string cronExpression)
        {
            JobType = jobType;
            CronExpression = cronExpression;
        }
    }
}
