using DiscordBot.Extensions;
using System;

namespace DiscordBot.Models
{
    /// <summary>
    /// 幫會一線牽關係
    /// </summary>
    public class OneLineBond
    {
        public int BondId { get; set; } // PK (遞增流水號)

        // 幫會內的會員
        public long DiscordId { get; set; } // FK
        public string MemberName { get; set; } // 會員名稱
        public GuildMember Member { get; set; } // 對應會員

        // 一線牽對象
        public long? PartnerId { get; set; } // 可能是幫會內的玩家
        public string PartnerName { get; set; } // 角色名稱

        public DateTime UpdateTime { get; set; } = DateTime.UtcNow; // 更新資料時間
    }
}
