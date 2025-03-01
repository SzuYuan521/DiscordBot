using System;
using DiscordBot.Enums;

namespace DiscordBot.Models
{
    /// <summary>
    /// 用於發送訊息
    /// </summary>
    public class DiscordMessage
    {
        public int Id { get; set; } // 流水號
        public ulong ChannelId { get; set; } // Discord 頻道 ID
        public string Content { get; set; } // 訊息內容
        public MessageType Type { get; set; } // 訊息類型
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 建立時間
        public DateTime UpdateAt { get; set; } = DateTime.UtcNow; // 更新時間
    }
}
