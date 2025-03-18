namespace DiscordBot.Models
{
    /// <summary>
    /// 幫會成員統計資訊
    /// </summary>
    public class MemberStatistics
    {

        /// <summary>
        /// Discord 使用者 ID
        /// </summary>
        public long DiscordId { get; set; }

        /// <summary>
        /// PVP 裝備 是否裝備泰山移
        /// </summary>
        public bool TaishanMove { get; set; }

        /// <summary>
        /// 關聯的 GuildMember
        /// </summary>
        public GuildMember GuildMember { get; set; }
    }
}
