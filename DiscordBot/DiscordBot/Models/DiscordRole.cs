using DiscordBot.Enums;

namespace DiscordBot.Models
{
    public class DiscordRole
    {
        public ulong Id { get; set; }
        public string RoleName { get; set; }

        /// <summary>
        /// 角色類型
        /// </summary>
        public DiscordRoleType DiscordRoleType { get; set; }
    }
}
