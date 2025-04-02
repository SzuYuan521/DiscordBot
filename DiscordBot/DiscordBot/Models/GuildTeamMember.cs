namespace DiscordBot.Models
{
    /// <summary>
    /// 幫會聯賽成員
    /// </summary>
    public class GuildTeamMember
    {
        public int Id { get; set; }
        public int GuildTeamId { get; set; }
        public long DiscordMemberId { get; set; } // 對應 GuildMembers.DiscordId
        public int Position { get; set; } // 0~7 代表小隊中第幾位，請假名單可忽略排序
        public GuildTeam GuildTeam { get; set; }
    }
}
