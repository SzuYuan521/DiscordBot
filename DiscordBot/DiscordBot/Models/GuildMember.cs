using DiscordBot.Enums;

namespace DiscordBot.Models
{
    public class GuildMember
    {
        /// <summary>
        /// Discord 使用者 ID (唯一)
        /// </summary>
        public long DiscordId { get; set; }  // 這是 Discord 使用者的 ID

        /// <summary>
        /// Discord 使用者名稱
        /// </summary>
        public string DiscordName { get; set; }

        /// <summary>
        /// 伺服器內的暱稱
        /// </summary>
        public string MemberName { get; set; }

        /// <summary>
        /// 遊戲職業
        /// </summary>
        public CharacterClassType? CharacterClass { get; set; }

        /// <summary>
        /// 會員加入幫會 DC 的時間
        /// </summary>
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 成員的統計資訊
        /// </summary>
        public MemberStatistics Statistics { get; set; }

        // 一個會員可以擁有多個一線牽
        public List<OneLineBond> OneLineBonds { get; set; } = new();
    }
}
