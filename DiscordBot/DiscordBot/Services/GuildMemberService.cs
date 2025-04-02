using Discord;
using Discord.WebSocket;
using DiscordBot.Data;
using DiscordBot.Enums;
using DiscordBot.Extensions;
using DiscordBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DiscordBot.Services
{
    public class GuildMemberService
    {
        private readonly ApplicationDbContext _dbContext;

        public GuildMemberService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 取得所有幫會成員
        /// </summary>
        public async Task<List<GuildMember>> GetAllMembersAsync()
        {
            return await _dbContext.GuildMembers.ToListAsync();
        }

        /// <summary>
        /// 根據 Discord ID 取得幫會成員
        /// </summary>
        public async Task<GuildMember?> GetMemberByDiscordIdAsync(long discordId)
        {
            return await _dbContext.GuildMembers.FirstOrDefaultAsync(m => m.DiscordId == discordId);
        }

        /// <summary>
        /// 新增幫會成員
        /// </summary>
        public async Task AddMemberAsync(GuildMember member)
        {
            await _dbContext.GuildMembers.AddAsync(member);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 更新幫會成員資訊
        /// </summary>
        public async Task UpdateMemberAsync(GuildMember member)
        {
            _dbContext.GuildMembers.Update(member);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 刪除幫會成員
        /// </summary>
        public async Task DeleteMemberAsync(long discordId)
        {
            var member = await _dbContext.GuildMembers.FirstOrDefaultAsync(m => m.DiscordId == discordId);
            if (member != null)
            {
                _dbContext.GuildMembers.Remove(member);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 取得職業對應中文名稱、色碼、身分組 ID
        /// </summary>
        public async Task<Dictionary<CharacterClassType, (string DisplayName, string Color, ulong RoleId)>> GetCharacterClassInfoMapAsync()
        {
            var nameMap = new Dictionary<CharacterClassType, string>
            {
                { CharacterClassType.Diviner, "神相" },
                { CharacterClassType.SpiritualHealer, "素問" },
                { CharacterClassType.Ironclad, "鐵衣" },
                { CharacterClassType.Spiritcaller, "九靈" },
                { CharacterClassType.Dreambreaker, "碎夢" },
                { CharacterClassType.Bloodriver, "血河" },
                { CharacterClassType.Dragonsong, "龍吟" }
            };

            var colorMap = new Dictionary<CharacterClassType, string>
            {
                { CharacterClassType.Diviner, "#4c64e8" },
                { CharacterClassType.SpiritualHealer, "#eeaeae" },
                { CharacterClassType.Ironclad, "#f5c67e" },
                { CharacterClassType.Spiritcaller, "#8a47ef" },
                { CharacterClassType.Dreambreaker, "#a2dddb" },
                { CharacterClassType.Bloodriver, "#f06260" },
                { CharacterClassType.Dragonsong, "#65e3b4" }
            };

            var dbRoles = await _dbContext.DiscordRoles
                .Where(r => r.DiscordRoleType == DiscordRoleType.CharacterClass)
                .ToListAsync();

            var result = new Dictionary<CharacterClassType, (string DisplayName, string Color, ulong RoleId)>();

            foreach (var pair in nameMap)
            {
                var match = dbRoles.FirstOrDefault(r => r.RoleName.Contains(pair.Value));

                if (match != null && colorMap.TryGetValue(pair.Key, out var color))
                {
                    result[pair.Key] = (pair.Value, color, match.Id);
                }
            }

            Console.WriteLine($"成功配對 {result.Count} 個職業角色身分組");
            return result;
        }


        /// <summary>
        /// 更新所有幫會成員的 Discord ID 和 MemberName
        /// </summary>
        public async Task UpdateGuildMembers(SocketGuild guild)
        {
            if (guild == null)
            {
                Console.WriteLine("❌ 找不到指定的伺服器！");
                return;
            }

            Console.WriteLine($"✅ 讀取伺服器：{guild.Name}");

            // 獲取所有成員
            var members = guild.Users;
            var dbMembers = await _dbContext.GuildMembers.ToListAsync(); // 取得資料庫中的所有成員
            // 取得職業相關身份組資訊
            var classInfoMap = await GetCharacterClassInfoMapAsync();
            var roleIdToClassMap = classInfoMap.ToDictionary(c => c.Value.RoleId, c => c.Key);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"📋 更新 {guild.Name} 伺服器成員資料 (共 {members.Count} 人):\n");

            HashSet<long> validMemberIds = new HashSet<long>(); // 記錄仍然有效的成員ID

            foreach (var member in members)
            {
                string nickname = string.IsNullOrEmpty(member.DisplayName) 
                    ? member.Username.ExtractCleanName() 
                    : member.DisplayName.ExtractCleanName();
                bool hasValidRole = member.Roles.Any(r =>
                    r.Id == 1335806149651464222 || 
                    r.Id == 1335798409688518657 || 
                    r.Id == 1335798371272753223);
                string roles = hasValidRole
                    ? string.Join(", ", member.Roles
                        .Where(r => r.Id == 1335806149651464222 || r.Id == 1335798409688518657 || r.Id == 1335798371272753223)
                        .Select(r => r.Name))
                    : "無身分組";

                // 檢查此使用者身上是否有職業身份組
                var classRole = member.Roles.FirstOrDefault(r => roleIdToClassMap.ContainsKey(r.Id));
                var detectedClass = classRole != null ? roleIdToClassMap[classRole.Id] : CharacterClassType.None;

                var existingMember = await _dbContext.GuildMembers.FindAsync((long)member.Id);

                if (existingMember == null && hasValidRole)
                {
                    // 新增新成員
                    _dbContext.GuildMembers.Add(new GuildMember
                    {
                        DiscordId = (long)member.Id,
                        DiscordName = $"{member.Username}#{member.Discriminator}",
                        MemberName = nickname,
                        CharacterClass = detectedClass,
                        JoinDate = DateTime.UtcNow,
                    });

                    sb.AppendLine($"✅ 新增成員: {nickname} ({member.Username}#{member.Discriminator}) | 身分組: {roles}");

                    // 查詢目前請假名單已有的最大 position
                    var maxPosition = _dbContext.GuildTeamMembers
                        .Where(tm => tm.GuildTeamId == 0)
                        .Select(tm => (int?)tm.Position)
                        .Max() ?? -1; // 若沒人, 預設 -1

                    // 新增 GuildTeamMember
                    _dbContext.GuildTeamMembers.Add(new GuildTeamMember
                    {
                        GuildTeamId = 0,
                        DiscordMemberId = (long)member.Id,
                        Position = 0 // 或 -1 代表尚未編排
                    });
                    sb.AppendLine($"分配至預設隊伍(請假名單): {nickname}");
                }
                else if (existingMember != null)
                {
                    if (!hasValidRole)
                    {
                        // 如果該成員沒有符合的身分組，則移除
                        _dbContext.GuildMembers.Remove(existingMember);
                        sb.AppendLine($"❌ 移除成員: {nickname} ({member.Username}#{member.Discriminator}) | 原因: 無符合的身分組");
                    }
                    else
                    {
                        // 更新已存在成員的資料
                        bool updated = false;

                        if (existingMember.MemberName != nickname)
                        {
                            existingMember.MemberName = nickname;
                            updated = true;
                        }

                        if (existingMember.DiscordName != $"{member.Username}#{member.Discriminator}")
                        {
                            existingMember.DiscordName = $"{member.Username}#{member.Discriminator}";
                            updated = true;
                        }

                        if(existingMember.CharacterClass != detectedClass)
                        {
                            existingMember.CharacterClass = detectedClass;
                            updated = true;
                        }

                        if (updated)
                        {
                            existingMember.UpdateTime = DateTime.UtcNow;
                            sb.AppendLine($"🔄 更新成員: {nickname} ({member.Username}#{member.Discriminator})");
                        }

                        validMemberIds.Add((long)member.Id); // 保留此成員
                    }
                }
            }

            // 刪除資料庫中不存在於 Discord 的成員
            foreach (var dbMember in dbMembers)
            {
                if (!validMemberIds.Contains(dbMember.DiscordId))
                {
                    _dbContext.GuildMembers.Remove(dbMember);
                    sb.AppendLine($"❌ 移除成員: {dbMember.MemberName} (ID: {dbMember.DiscordId}) | 原因: 已不是幫眾");

                    var teamMembersToRemove = await _dbContext.GuildTeamMembers
                        .Where(tm => tm.DiscordMemberId == dbMember.DiscordId)
                        .ToListAsync();

                    if (teamMembersToRemove.Any())
                    {
                        _dbContext.GuildTeamMembers.RemoveRange(teamMembersToRemove);
                        foreach (var tm in teamMembersToRemove)
                        {
                            sb.AppendLine($"從隊伍中移除成員 {dbMember.MemberName} ");
                        }
                    }
                }
            }

            // 儲存變更
            await _dbContext.SaveChangesAsync();
            Console.WriteLine(sb.ToString()); // 在控制台輸出
        }

        /// <summary>
        /// 補登泰山移
        /// </summary>
        /// <returns></returns>
        public async Task ReloadTaishanMoveData(SocketGuild guild)
        {
            if (guild == null)
            {
                Console.WriteLine("❌ 找不到指定的伺服器！");
                return;
            }

            Console.WriteLine($"🔄 重新讀取 {guild.Name} 的泰山移表情數據");

            // 從資料庫查找所有有監聽的泰山移訊息
            var monitoredMessages = await _dbContext.MonitoredMessages
                .Where(m => m.MessageType == Enums.MessageType.Statistics) // 限制只查找統計類型的監聽訊息
                .ToListAsync();

            if (!monitoredMessages.Any())
            {
                Console.WriteLine("⚠️ 沒有找到已監聽的泰山移訊息！");
                return;
            }

            Console.WriteLine($"🔍 共找到 {monitoredMessages.Count} 則泰山移監聽訊息");

            foreach (var monitoredMessage in monitoredMessages)
            {
                var channel = guild.GetTextChannel(monitoredMessage.ChannelId);
                if (channel == null)
                {
                    Console.WriteLine($"⚠️ 無法取得頻道 {monitoredMessage.ChannelId}，跳過！");
                    continue;
                }

                var message = await channel.GetMessageAsync(monitoredMessage.MessageId);
                if (message == null)
                {
                    Console.WriteLine($"⚠️ 找不到訊息 {monitoredMessage.MessageId}，跳過！");
                    continue;
                }

                // 讀取 StatisticsConfig 取得正確的表情名稱
                var taishanMoveConfig = await _dbContext.StatisticsConfigs
                    .FirstOrDefaultAsync(s => s.StatisticsType == StatisticsType.TaishanMove);

                if (taishanMoveConfig == null)
                {
                    Console.WriteLine("⚠️ 沒有找到泰山移的表情設定，請確認 StatisticsConfig 是否正確設置！");
                    return;
                }

                string taishanMoveEmoji = taishanMoveConfig.Emoji; // 取得資料庫中設定的表情符號
                Console.WriteLine($"✅ 讀取泰山移表情符號: {taishanMoveEmoji}");

                // 取得這則訊息的所有表情回應
                foreach (var reaction in message.Reactions)
                {
                    if (reaction.Key.Name == taishanMoveEmoji) // 確保表情是泰山移
                    {
                        var users = await message.GetReactionUsersAsync(reaction.Key, 100).FlattenAsync(); // 取得表情回應的用戶
                        foreach (var user in users)
                        {
                            if (user.IsBot) continue; // 忽略機器人

                            var existingMember = await _dbContext.GuildMembers.FindAsync((long)user.Id);
                            if (existingMember == null)
                            {
                                // 補紀錄到資料庫
                                var newMember = new GuildMember
                                {
                                    DiscordId = (long)user.Id,
                                    DiscordName = user.Username,
                                    MemberName = user.Username,
                                    CharacterClass = CharacterClassType.None,
                                    JoinDate = DateTime.UtcNow
                                };

                                _dbContext.GuildMembers.Add(newMember);
                                Console.WriteLine($"✅ 新增成員 {user.Username} (ID: {user.Id})");
                            }

                            // 更新統計
                            var memberStats = await _dbContext.MemberStatistics.FindAsync((long)user.Id);
                            if (memberStats == null)
                            {
                                memberStats = new MemberStatistics { DiscordId = (long)user.Id, TaishanMove = true };
                                _dbContext.MemberStatistics.Add(memberStats);
                            }
                            else
                            {
                                memberStats.TaishanMove = true;
                            }
                        }
                    }
                }

                await _dbContext.SaveChangesAsync();
                Console.WriteLine("✅ 泰山移數據同步完成！");
            }
        }
    }
}