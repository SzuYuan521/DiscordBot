using DiscordBot.Enums;

namespace DiscordBot.Models
{
    /// <summary>
    /// 幻彩靈契, 點選表情獲得身份組
    /// </summary>
    public class RoleMagicPact
    {
        public int Id { get; set; }

        /// <summary>
        /// 頻道 ID
        /// </summary>
        public ulong DiscordChannelId { get; set; }  // FK
        public DiscordChannel? DiscordChannel { get; set; }

        /// <summary>
        /// 訊息 ID
        /// </summary>
        public ulong MonitoredMessageId { get; set; }  // FK
        public MonitoredMessage? MonitoredMessage { get; set; }

        /// <summary>
        /// 點擊的表情符號
        /// </summary>
        public string? Emoji { get; set; }

        /// <summary>
        /// 角色 ID
        /// </summary>
        public ulong DiscordRoleId { get; set; }
        public DiscordRole? DiscordRole { get; set; }
    }
}
