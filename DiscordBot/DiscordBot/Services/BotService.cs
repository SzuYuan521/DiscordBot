using Discord;
using Discord.WebSocket;
using DiscordBot.Data;
using DiscordBot.Models;
using DiscordBot.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;

namespace DiscordBot.Services
{
    /// <summary>
    /// Discord 機器人服務, 負責監聽訊息, 表情回應, 管理身分組
    /// </summary>
    public class BotService
    {
        private readonly ILogger<BotService> _logger;
        private readonly CommandService _commandService;
        private readonly DiscordSocketClient _client; // Discord 機器人 client 端
        private readonly IServiceScopeFactory _scopeFactory; // 產生新的 DbContext 範圍

        // 記錄監聽的訊息 ID 與對應的 MonitoredMessage 物件
        private Dictionary<ulong, MonitoredMessage> _monitoredMessages = new();
        private Dictionary<string, ulong> _reactionRoleMap = new(); // 表情對應的身分組

        public BotService(ILogger<BotService> logger, CommandService commandService, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _commandService = commandService;

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,  // 使用全部的 Intents
                AlwaysDownloadUsers = true  // 確保機器人可以獲取所有伺服器用戶資料
            };
            _client = new DiscordSocketClient(config);

            // 設定監聽表情回應的事件
            _client.ReactionAdded += OnReactionAdded;
            _client.ReactionRemoved += OnReactionRemoved;

            // 監聽用戶加入事件
            _client.UserJoined += OnUserJoined;

            //      _client.Ready += OnBotReady; // 用來補建成員資料
        }

        /// <summary>
        /// 啟動機器人並連接到 Discord 伺服器
        /// </summary>
        public async Task StartAsync()
        {
            _client.Log += LogAsync;
            _client.MessageReceived += MessageReceivedAsync;

            // 從資料庫載入表情符號對應的身分組
            await LoadReactionRolesFromDatabase();

            // 取得 (Render) 環境變數中的 Discord Token
            string discordBotToken = Environment.GetEnvironmentVariable("DISCORDBOT_TOKEN");

            if (string.IsNullOrEmpty(discordBotToken))
            {
                throw new Exception("Discord token is missing!");
            }

            // 登入並啟動機器人
            await _client.LoginAsync(TokenType.Bot, discordBotToken);
            await _client.StartAsync();

            // 防止方法立即結束, 讓機器人保持運行
            await Task.Delay(-1);
        }

        /// <summary>
        /// 從資料庫載入 Discord Role 資訊
        /// </summary>
        private async Task LoadReactionRolesFromDatabase()
        {
            using (var scope = _scopeFactory.CreateScope()) // 創建新的 Scoped
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // 取得 ApplicationDbContext
                var roleMappings = await dbContext.RoleMagicPacts
                    .Include(r => r.DiscordRole) // 對應的 Discord 身分組
                    .Include(r => r.MonitoredMessage) // 監聽的訊息
                    .Include(r => r.DiscordChannel)// 頻道資訊
                    .ToListAsync();

                var monitoredMessages = await dbContext.MonitoredMessages.ToListAsync();

                _reactionRoleMap.Clear();
                _monitoredMessages.Clear();

                foreach (var mapping in roleMappings)
                {
                    if (mapping.Emoji != null)
                    {
                        _reactionRoleMap[mapping.Emoji] = mapping.DiscordRoleId; // 儲存表情與身分組的對應關係
                    }
                }

                foreach (var msg in monitoredMessages)
                {
                    _monitoredMessages[msg.MessageId] = msg;
                }

                _logger.LogInformation("已成功從資料庫加載 Reaction Role 設定和監聽訊息");
            }
        }

        private Task LogAsync(LogMessage log)
        {
            _logger.LogInformation(log.Message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 取得 Discord Client 端實例
        /// </summary>
        public DiscordSocketClient GetClient()
        {
            return _client;
        }

        /// <summary>
        /// 當新成員加入時, 記錄到資料庫
        /// </summary>
        private async Task OnUserJoined(SocketGuildUser user)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var guildMemberService = scope.ServiceProvider.GetRequiredService<GuildMemberService>();

                var existingMember = await guildMemberService.GetMemberByDiscordIdAsync((long)user.Id);
                if (existingMember == null)
                {
                    var newMember = new GuildMember
                    {
                        DiscordId = (long)user.Id,
                        DiscordName = user.Username,
                        CharacterClass = CharacterClassType.None,
                        JoinDate = DateTime.UtcNow
                    };

                    await guildMemberService.AddMemberAsync(newMember);
                    Console.WriteLine($"新成員 {user.Username} 已加入，並記錄至資料庫");
                }
                else
                {
                    Console.WriteLine($"成員 {user.Username} 已經在資料庫中");
                }
            }
        }


        /// <summary>
        /// 監聽訊息, 處理指令
        /// </summary>
        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.IsBot)
                return;

            if (message.Content.StartsWith("/"))
            {
                var response = await _commandService.GetResponse(message.Content);

                if (!string.IsNullOrEmpty(response))
                {
                    await message.Channel.SendMessageAsync(response);
                }
                else
                {
                    await message.Channel.SendMessageAsync("未找到對應的指令");
                }
            }
        }


        /// <summary>
        /// 發送訊息到指定頻道 ID
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessageToChannel(ulong channelId, string message)
        {
            // 取得指定頻道
            var channel = _client.GetChannel(channelId) as ITextChannel;

            // 確保頻道是文字頻道
            if (channel != null)
            {
                await channel.SendMessageAsync(message); // 發送訊息
            }
            else
            {
                // 頻道無效
                Console.WriteLine("指定的頻道 ID 無效！");
            }
        }

        public async Task DeleteMessageFromChannel(ulong channelId, ulong messageId)
        {
            // 取得指定的頻道
            var channel = _client.GetChannel(channelId) as ITextChannel;

            if (channel != null)
            {
                try
                {
                    // 取得指定訊息並刪除
                    var message = await channel.GetMessageAsync(messageId);
                    if (message != null)
                    {
                        await message.DeleteAsync();
                        Console.WriteLine("訊息刪除成功！");
                    }
                    else
                    {
                        Console.WriteLine("未找到指定的訊息！");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"刪除訊息時發生錯誤: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("指定的頻道 ID 無效！");
            }
        }

        /// <summary>
        /// 監聽增加表情回應, 添加對應的身分組
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="channel"></param>
        /// <param name="reaction"></param>
        /// <returns></returns>
        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            try
            {
                if (!_monitoredMessages.TryGetValue(reaction.MessageId, out var monitoredMessage))
                    return;

                // 嚴格比對 監聽的頻道 ID 是否與當前頻道相符
                if (monitoredMessage.ChannelId != reaction.Channel.Id)
                    return;

                switch (monitoredMessage.MessageType)
                {
                    case Enums.MessageType.ReactionRole:
                        await HandleReactionRoleAdded(reaction, monitoredMessage);
                        break;

                    case Enums.MessageType.Statistics:
                        await HandleStatisticsReactionAdded(reaction, monitoredMessage);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"處理反應時發生錯誤: {ex.Message}");
                Console.WriteLine($"錯誤詳情: {ex}");
            }

        }

        /// <summary>
        /// 監聽移除表情回應, 移除身分組
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="channel"></param>
        /// <param name="reaction"></param>
        /// <returns></returns>
        private async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            try
            {
                if (!_monitoredMessages.TryGetValue(reaction.MessageId, out var monitoredMessage))
                    return;

                // 嚴格比對 監聽的頻道 ID 是否與當前頻道相符
                if (monitoredMessage.ChannelId != reaction.Channel.Id)
                    return;

                switch (monitoredMessage.MessageType)
                {
                    case Enums.MessageType.ReactionRole:
                        await HandleReactionRoleRemoved(reaction, monitoredMessage);
                        break;

                    case Enums.MessageType.Statistics:
                        await HandleStatisticsReactionRemoved(reaction, monitoredMessage);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"處理反應時發生錯誤: {ex.Message}");
            }
        }
 

        /// <summary>
        /// 當用戶在監聽訊息上增加表情時, 根據 ReactionRole 設定分配身分組
        /// </summary>
        private async Task HandleReactionRoleAdded(SocketReaction reaction, MonitoredMessage monitoredMessage)
        {
            if (!_reactionRoleMap.TryGetValue(reaction.Emote.Name, out ulong roleId))
            {
                Console.WriteLine($"表情 {reaction.Emote.Name} 沒有對應的身份組");
                return;
            }

            // 檢查 reaction 的訊息 ID 和頻道 ID 是否與 MonitoredMessage 相符
            if (monitoredMessage.MessageId != reaction.MessageId || monitoredMessage.ChannelId != reaction.Channel.Id)
                return;

            var guild = (reaction.Channel as SocketGuildChannel)?.Guild;
            var user = guild?.GetUser(reaction.UserId);
            if (user != null)
            {
                var role = guild.GetRole(roleId);
                if (role != null)
                {
                    try
                    {
                        await user.AddRoleAsync(role);
                        Console.WriteLine($"已給 {user.Username} 添加身分組 {role.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"添加身分組時發生錯誤: {ex.Message}");
                        Console.WriteLine($"錯誤詳情: {ex}");
                    }
                }
            }
        }


        /// <summary>
        /// 當用戶在監聽訊息上移除表情時, 根據 ReactionRole 設定移除身分組
        /// </summary>
        private async Task HandleReactionRoleRemoved(SocketReaction reaction, MonitoredMessage monitoredMessage)
        {
            if (!_reactionRoleMap.TryGetValue(reaction.Emote.Name, out ulong roleId))
            {
                Console.WriteLine($"表情 {reaction.Emote.Name} 沒有對應的身份組");
                return;
            }

            // 檢查 reaction 的訊息 ID 和頻道 ID 是否與 MonitoredMessage 相符
            if (monitoredMessage.MessageId != reaction.MessageId || monitoredMessage.ChannelId != reaction.Channel.Id)
                return;

            var guild = (reaction.Channel as SocketGuildChannel)?.Guild;
            var user = guild?.GetUser(reaction.UserId);
            if (user != null)
            {
                var role = guild.GetRole(roleId);
                if (role != null)
                {
                    try
                    {
                        await user.RemoveRoleAsync(role);
                        Console.WriteLine($"已移除 {user.Username} 的身分組 {role.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"移除身分組時發生錯誤: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 根據 Emoji 更新統計數據 (增加)
        /// </summary>
        /// <param name="reaction"></param>
        /// <returns></returns>
        private async Task HandleStatisticsReactionAdded(SocketReaction reaction, MonitoredMessage monitoredMessage)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 從資料庫讀取 Emoji 與統計類型的對應關係
            var config = await dbContext.StatisticsConfigs.FirstOrDefaultAsync(s => s.Emoji == reaction.Emote.Name);
            if (config == null) return; // 沒有對應的統計類型, 直接返回

            // 確保此表情僅影響對應的 MonitoredMessage
            if (monitoredMessage.MessageId != reaction.MessageId || monitoredMessage.ChannelId != reaction.Channel.Id)
                return;

            var memberStats = await dbContext.MemberStatistics.FindAsync(reaction.UserId);
            if (memberStats == null)
            {
                memberStats = new MemberStatistics { DiscordId = (long)reaction.UserId };
                dbContext.MemberStatistics.Add(memberStats);
            }

            switch (config.StatisticsType)
            {
                case StatisticsType.TaishanMove: // 泰山移
                    memberStats.TaishanMove = true;
                    break;
            }

            await dbContext.SaveChangesAsync();
            Console.WriteLine($"✅ 更新統計數據: {reaction.UserId} => {config.StatisticsType}");
        }

        /// <summary>
        /// 根據 Emoji 更新統計數據 (減少)
        /// </summary>
        /// <param name="reaction"></param>
        /// <returns></returns>
        private async Task HandleStatisticsReactionRemoved(SocketReaction reaction, MonitoredMessage monitoredMessage)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 從資料庫讀取 Emoji 與統計類型的對應關係
            var config = await dbContext.StatisticsConfigs.FirstOrDefaultAsync(s => s.Emoji == reaction.Emote.Name);
            if (config == null) return; // 沒有對應的統計類型, 直接返回

            // 確保此表情僅影響對應的 MonitoredMessage
            if (monitoredMessage.MessageId != reaction.MessageId || monitoredMessage.ChannelId != reaction.Channel.Id)
                return;

            var memberStats = await dbContext.MemberStatistics.FindAsync(reaction.UserId);
            if (memberStats == null) return;

            switch (config.StatisticsType)
            {
                case StatisticsType.TaishanMove:
                    memberStats.TaishanMove = false;
                    break;
            }

            await dbContext.SaveChangesAsync();
            Console.WriteLine($"❌ 更新統計數據: {reaction.UserId} => {config.StatisticsType} (已移除)");
        }

        /// <summary>
        /// 補登泰山移
        /// </summary>
        /// <returns></returns>
        public async Task ReloadTaishanMoveData()
        {
            var guild = _client.GetGuild(1335798324275449929); // 你的伺服器 ID
            if (guild == null)
            {
                Console.WriteLine("❌ 找不到指定的伺服器！");
                return;
            }

            Console.WriteLine($"🔄 重新讀取 {guild.Name} 的泰山移表情數據");

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // 從資料庫查找所有有監聽的泰山移訊息
                var monitoredMessages = await dbContext.MonitoredMessages
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
                    var taishanMoveConfig = await dbContext.StatisticsConfigs
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

                                var existingMember = await dbContext.GuildMembers.FindAsync((long)user.Id);
                                if (existingMember == null)
                                {
                                    // **補紀錄到資料庫**
                                    var newMember = new GuildMember
                                    {
                                        DiscordId = (long)user.Id,
                                        DiscordName = user.Username,
                                        MemberName = user.Username, // 暫時用 Discord Name，稍後可更新
                                        CharacterClass = CharacterClassType.None,
                                        JoinDate = DateTime.UtcNow
                                    };

                                    dbContext.GuildMembers.Add(newMember);
                                    Console.WriteLine($"✅ 新增成員 {user.Username} (ID: {user.Id})");
                                }

                                // 更新統計
                                var memberStats = await dbContext.MemberStatistics.FindAsync((long)user.Id);
                                if (memberStats == null)
                                {
                                    memberStats = new MemberStatistics { DiscordId = (long)user.Id, TaishanMove = true };
                                    dbContext.MemberStatistics.Add(memberStats);
                                }
                                else
                                {
                                    memberStats.TaishanMove = true;
                                }
                            }
                        }
                    }
                }

                await dbContext.SaveChangesAsync();
                Console.WriteLine("✅ 泰山移數據同步完成！");
            }
        }

        /// <summary>
        /// 更新所有幫會成員的 Discord ID 和 MemberName
        /// </summary>
        public async Task UpdateGuildMembers()
        {
            var guild = _client.GetGuild(1335798324275449929);
            if (guild == null)
            {
                Console.WriteLine("❌ 找不到指定的伺服器！");
                return;
            }

            Console.WriteLine($"✅ 讀取伺服器：{guild.Name}");

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // 獲取所有成員
                var members = guild.Users;
                var dbMembers = await dbContext.GuildMembers.ToListAsync(); // 取得資料庫中的所有成員

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"📋 **更新 {guild.Name} 伺服器成員資料** (共 {members.Count} 人):\n");

                HashSet<long> validMemberIds = new HashSet<long>(); // 記錄仍然有效的成員ID

                foreach (var member in members)
                {
                    string nickname = string.IsNullOrEmpty(member.DisplayName) ? member.Username : member.DisplayName;
                    bool hasValidRole = member.Roles.Any(r => r.Id == 1335806149651464222 || r.Id == 1335798409688518657 || r.Id == 1335798371272753223);
                    string roles = hasValidRole
                        ? string.Join(", ", member.Roles
                            .Where(r => r.Id == 1335806149651464222 || r.Id == 1335798409688518657 || r.Id == 1335798371272753223)
                            .Select(r => r.Name))
                        : "無身分組";

                    var existingMember = await dbContext.GuildMembers.FindAsync((long)member.Id);

                    if (existingMember == null && hasValidRole)
                    {
                        // **新增新成員**
                        dbContext.GuildMembers.Add(new GuildMember
                        {
                            DiscordId = (long)member.Id,
                            DiscordName = $"{member.Username}#{member.Discriminator}",
                            MemberName = nickname,
                            CharacterClass = CharacterClassType.None,
                            JoinDate = DateTime.UtcNow,
                        });

                        sb.AppendLine($"✅ 新增成員: {nickname} ({member.Username}#{member.Discriminator}) | 身分組: {roles}");
                    }
                    else if (existingMember != null)
                    {
                        if (!hasValidRole)
                        {
                            // **如果該成員沒有符合的身分組，則移除**
                            dbContext.GuildMembers.Remove(existingMember);
                            sb.AppendLine($"❌ 移除成員: {nickname} ({member.Username}#{member.Discriminator}) | 原因: 無符合的身分組");
                        }
                        else
                        {
                            // **更新已存在成員的資料**
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

                            if (updated)
                            {
                                sb.AppendLine($"🔄 更新成員: {nickname} ({member.Username}#{member.Discriminator})");
                            }

                            validMemberIds.Add((long)member.Id); // 保留此成員
                        }
                    }
                }

                // **刪除資料庫中不存在於 Discord 的成員**
                foreach (var dbMember in dbMembers)
                {
                    if (!validMemberIds.Contains(dbMember.DiscordId))
                    {
                        dbContext.GuildMembers.Remove(dbMember);
                        sb.AppendLine($"❌ 移除成員: {dbMember.MemberName} (ID: {dbMember.DiscordId}) | 原因: 已不是幫眾");
                    }
                }

                // 儲存變更
                await dbContext.SaveChangesAsync();
                Console.WriteLine(sb.ToString()); // 在控制台輸出
            }
        }
    }
}
