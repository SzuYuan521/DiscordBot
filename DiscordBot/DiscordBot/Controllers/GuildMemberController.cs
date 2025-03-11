using DiscordBot.Data;
using DiscordBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DiscordBot.Enums;
using DiscordBot.Dtos;

namespace DiscordBot.Controllers
{
    public class GuildMemberController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public GuildMemberController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得統計類型選單
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> GetStatisticsTypes()
        {
            var types = await _dbContext.StatisticsConfigs
                .Select(s => new { s.StatisticsType, s.StatisticsName }) // 使用統計類型和名稱
                .ToListAsync();

            return Json(types);
        }

        /// <summary>
        /// 依統計類型取得成員
        /// </summary>
        /// <param name="statisticsType"></param>
        /// <returns></returns>
        public async Task<IActionResult> GetMembersByStatisticsType(int statisticsType)
        {
            List<GuildMemberDto> members = new();
            var statisticsName = string.Empty;  // 統計類型名稱
            var memberCount = 0;  // 符合條件的成員數量

            switch ((StatisticsType)statisticsType)
            {
                case StatisticsType.TaishanMove:
                    // 只取得裝備了泰山移的成員
                    var memberList = await _dbContext.MemberStatistics
                        .Include(m => m.GuildMember)  // 確保GuildMember關聯載入
                        .Where(m => m.TaishanMove && m.GuildMember != null)
                        .Select(m => new GuildMemberDto
                        {
                            MemberName = m.GuildMember.MemberName,  // 伺服器內的暱稱
                            DiscordId = m.GuildMember.DiscordId     // Discord ID
                        })
                        .ToListAsync();

                    members = memberList;
                    statisticsName = "泰山移";  // 設定統計名稱
                    memberCount = members.Count;  // 計算符合條件的成員數量
                    break;

                default:
                    return Json(new { error = "未知的統計類型" });
            }

            return Json(new
            {
                statisticsName,
                memberCount,
                members
            });
        }

    }
}
