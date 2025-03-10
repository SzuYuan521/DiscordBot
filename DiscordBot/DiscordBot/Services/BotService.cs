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

            _client.Ready += OnBotReady; // 用來補建成員資料
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

                var existingMember = await guildMemberService.GetMemberByDiscordIdAsync(user.Id);
                if (existingMember == null)
                {
                    var newMember = new GuildMember
                    {
                        DiscordId = user.Id,
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

                switch (monitoredMessage.MessageType)
                {
                    case Enums.MessageType.ReactionRole:
                        await HandleReactionRoleAdded(reaction);
                        break;

                    case Enums.MessageType.Statistics:
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

                switch (monitoredMessage.MessageType)
                {
                    case Enums.MessageType.ReactionRole:
                        await HandleReactionRoleRemoved(reaction);
                        break;

                    case Enums.MessageType.Statistics:
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
        private async Task HandleReactionRoleAdded(SocketReaction reaction)
        {
            if (!_reactionRoleMap.TryGetValue(reaction.Emote.Name, out ulong roleId))
            {
                Console.WriteLine($"表情 {reaction.Emote.Name} 沒有對應的身份組");
                return;
            }

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
        private async Task HandleReactionRoleRemoved(SocketReaction reaction)
        {
            if (!_reactionRoleMap.TryGetValue(reaction.Emote.Name, out ulong roleId))
            {
                Console.WriteLine($"表情 {reaction.Emote.Name} 沒有對應的身份組");
                return;
            }

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
        /// 用來補建成員資料
        /// </summary>
        private async Task OnBotReady()
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
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"📋 **伺服器 {guild.Name} 成員列表** (總共 {members.Count} 人):\n");

                foreach (var member in members)
                {
                    string roles = member.Roles.Count > 1
                        ? string.Join(", ", member.Roles.Where(r => r.Id != 1335798324275449929).Select(r => r.Name))
                        : "無身分組";

                    sb.AppendLine($"🆔 {member.Id} | **{member.Username}#{member.Discriminator}** | {roles}");

                    // 檢查資料庫是否已經有該成員
                    var existingMember = await dbContext.GuildMembers.FindAsync(member.Id);
                    if (existingMember == null)
                    {
                        dbContext.GuildMembers.Add(new GuildMember
                        {
                            DiscordId = member.Id,
                            DiscordName = $"{member.Username}#{member.Discriminator}",
                            CharacterClass = CharacterClassType.None,
                            JoinDate = DateTime.UtcNow
                        });

                        Console.WriteLine($"✅ 新增成員 {member.Username}#{member.Discriminator} 至 GuildMembers");
                    }
                }

                // 儲存變更
                await dbContext.SaveChangesAsync();
                Console.WriteLine(sb.ToString()); // 在控制台輸出
            }
        }

    }
}
