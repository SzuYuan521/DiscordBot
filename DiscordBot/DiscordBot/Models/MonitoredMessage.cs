using DiscordBot.Enums;

namespace DiscordBot.Models
{
    public class MonitoredMessage
    {
        public int Id { get; set; }

        /// <summary>
        /// 被監聽的 Discord 訊息 ID(ulong)
        /// </summary>
        public ulong MessageId { get; set; }

        /// <summary>
        /// 訊息所在的頻道 ID
        /// </summary>
        public ulong ChannelId { get; set; }

        /// <summary>
        /// 訊息類型
        /// </summary>
        public MessageType MessageType { get; set; }
    }

}
