using DiscordBot.Enums;

namespace DiscordBot.Models
{
    /// <summary>
    /// 存放 Emoji 與統計類型的對應關係
    /// </summary>
    public class StatisticsConfig
    {
        public int Id { get; set; }

        /// <summary>
        /// Emoji (表情符號)
        /// </summary>
        public string Emoji { get; set; }

        /// <summary>
        /// 對應的統計類型
        /// </summary>
        public StatisticsType StatisticsType { get; set; }
    }
}
