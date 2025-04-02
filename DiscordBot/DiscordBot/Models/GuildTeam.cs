namespace DiscordBot.Models
{
    /// <summary>
    /// 幫會聯賽隊伍
    /// </summary>
    public class GuildTeam
    {
        public int Id { get; set; }
        public int TeamGroupId { get; set; }
        public string TeamName { get; set; } // 隊伍名稱
        public int? Order { get; set; } // 隊伍排序
        public TeamGroup TeamGroup { get; set; }
        public List<GuildTeamMember> TeamMembers { get; set; } = new();
    }
}
