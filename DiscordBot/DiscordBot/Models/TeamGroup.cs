namespace DiscordBot.Models
{
    /// <summary>
    /// 幫會聯賽團隊
    /// </summary>
    public class TeamGroup
    {
        public int Id { get; set; }
        public string GroupName { get; set; } // 一團、二團
        public List<GuildTeam> GuildTeams { get; set; } = new();
    }
}
