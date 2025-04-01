using DiscordBot.Data;
using DiscordBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DiscordBot.Enums;
using DiscordBot.Dtos;
using System.Diagnostics;
using DiscordBot.Services;
using DiscordBot.Extensions;

namespace DiscordBot.Controllers
{
    public class GuildMemberController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly BotService _botService;
        private readonly GuildMemberService _guildMemberService;
        private readonly GuildTeamService _guildTeamService;

        public GuildMemberController(ApplicationDbContext dbContext, BotService botService, GuildMemberService guildMemberService, GuildTeamService guildTeamService)
        {
            _dbContext = dbContext;
            _botService = botService;
            _guildMemberService = guildMemberService;
            _guildTeamService = guildTeamService;
            _guildTeamService = guildTeamService;
        }

        public IActionResult Index()
        {
            return View();
        }

        // 一線牽
        public IActionResult OneLineBond()
        {
            return View();
        }

        // 統計數據
        public IActionResult Statistics()
        {
            return View();
        }

        /// <summary>
        /// 顯示成員管理頁面（ManagingMembers.cshtml）
        /// </summary>
        [HttpGet]
        public IActionResult ManagingMembers()
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
                            DiscordId = (ulong)m.GuildMember.DiscordId     // Discord ID
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

        /// <summary>
        /// 取得所有幫會成員(用於成員清單)
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> GetAllMembers()
        {
            var classColorMap = await _guildMemberService.GetCharacterClassInfoMapAsync();

            var members = await _dbContext.GuildMembers
                .Select(m => new
                {
                    DiscordId = m.DiscordId.ToString(),
                    m.MemberName,
                    m.CharacterClass
                })
                .ToListAsync();

            var result = members.Select(m => new
            {
                m.DiscordId,
                m.MemberName,
                m.CharacterClass,
                Color = m.CharacterClass != CharacterClassType.None && classColorMap.ContainsKey((CharacterClassType)m.CharacterClass)
                            ? classColorMap[(CharacterClassType)m.CharacterClass].Color
                            : null
            });

            return Json(result);
        }

        /// <summary>
        /// 取得所有幫會成員(用於一線牽選單)
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> GetAllMembersName()
        {
            var members = await _dbContext.GuildMembers
                .Select(m => new
                {
                    DiscordId = m.DiscordId.ToString(), // 轉換為字串
                    m.MemberName
                })
                .ToListAsync();

            return Json(members);
        }

        /// <summary>
        /// 創建一線牽
        /// </summary>
        /// <param name="memberId1">成員1</param>
        /// <param name="memberId2">成員2</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateBond(string memberId1, string memberId2)
        {
            if (string.IsNullOrEmpty(memberId1) || string.IsNullOrEmpty(memberId2) || memberId1 == memberId2)
            {
                return Json(new { success = false, message = "請選擇不同的成員！" });
            }

            // 轉換為 long (確保前端傳來的 string 可以正常轉換)
            if (!long.TryParse(memberId1, out long member1Id) || !long.TryParse(memberId2, out long member2Id))
            {
                return Json(new { success = false, message = "成員 ID 格式錯誤！" });
            }

            var member1 = await _dbContext.GuildMembers.FirstOrDefaultAsync(m => m.DiscordId == member1Id);
            var member2 = await _dbContext.GuildMembers.FirstOrDefaultAsync(m => m.DiscordId == member2Id);

            if (member1 == null || member2 == null)
            {
                return Json(new { success = false, message = "某個成員不存在" });
            }

            // 檢查是否已存在這兩人的一線牽關係
            bool bondExists = await _dbContext.OneLineBonds.AnyAsync(b =>
                (b.DiscordId == member1Id && b.PartnerId == member2Id) ||
                (b.DiscordId == member2Id && b.PartnerId == member1Id));

            if (bondExists)
            {
                return Json(new { success = false, message = $"{member1.MemberName} 和 {member2.MemberName} 已經有一線牽關係！" });
            }

            // 新增一線牽關係
            var oneLineBond = new OneLineBond
            {
                DiscordId = member1.DiscordId,
                MemberName = member1.MemberName,
                PartnerId = member2.DiscordId,
                PartnerName = member2.MemberName,
                UpdateTime = DateTime.UtcNow,
            };

            _dbContext.OneLineBonds.Add(oneLineBond);
            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = $"{member1.MemberName} 和 {member2.MemberName} 已建立一線牽關係！" });
        }

        /// <summary>
        /// 取得所有一線牽關係
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> GetAllBonds()
        {
            var bonds = await _dbContext.OneLineBonds
                .Select(b => new
                {
                    MemberId = b.DiscordId,
                    MemberName = b.MemberName,
                    PartnerId = b.PartnerId,
                    PartnerName = b.PartnerName
                })
                .ToListAsync();

            return Json(bonds);
        }

        [HttpPost]
        public async Task<IActionResult> ReloadTaishanMoveData()
        {
            try
            {
                var guild = _botService.GetClient().GetGuild(1335798324275449929);
                await _guildMemberService.ReloadTaishanMoveData(guild);
                return Json(new { success = true, message = "泰山移表情數據已重新載入並同步至資料庫！" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"重新載入時發生錯誤: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateGuildMembers()
        {
            try
            {
                var guild = _botService.GetClient().GetGuild(1335798324275449929);
                await _guildMemberService.UpdateGuildMembers(guild);
                return Json(new { success = true, message = "更新所有幫會成員的 Discord ID 和 MemberName！" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"重新載入時發生錯誤: {ex.Message}" });
            }
        }
    }
}
