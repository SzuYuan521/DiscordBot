using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    public class BotService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BotService> _logger;
        private readonly CommandService _commandService;
        private readonly DiscordSocketClient _client;
        private readonly HashSet<ulong> _messageIds = new() { 1343996683037577266, 1344363767651237959 };  // 監聽這則訊息的表情
        private readonly Dictionary<string, ulong> _reactionRoleMap = new()  // 表情對應的身分組
        {
            { "🍎", 1343254054679351356 }, // 柔霧粉
            { "🍏", 1343257439134421033 }, // 玫瑰粉
            { "🌽", 1344000928537251851 }, // 女團粉
            { "🍐", 1343259625038020761 },  // 草莓奶霜
            { "🍊", 1343259151651962963 }, // 焦糖杏仁
            { "🍑", 1343948660731281439 }, // 柳橙橘
            { "🍋", 1343260172524585010 }, // 薰衣草
            { "🥭", 1343949670896111627 }, // 芋頭紫
            { "🍍", 1343949154367307868 }, // 紫羅蘭
            { "🍌", 1343258592572215378 }, // 碧湖藍
            { "🥥", 1343949664117985290 }, // 霧藍
            { "🥝", 1343950161407115264 }, // 蔚藍
            { "🍉", 1343258517280129055 }, // 海洋之星
            { "🍇", 1343259496713027585 }, // 抹茶奶霜
            { "🍓", 1343258841671929946 }, // 松花青
            { "🍅", 1343952020083576842 }, // 經典綠
            { "🍆", 1343952874278879232 }, // 墨綠
            { "🥑", 1343953205897465977 }, // 鮮黃
            { "🍈", 1343256277538574438 }, // 奶油黃
            { "🍒", 1343258062106136586 }, // 白巧克力
            { "🌶️", 1344351623060914216 }, // 藍莓牛奶
            { "🥕", 1344358121572667433 }, // 灰茶
            { "🥒", 1344358499207090186 }, // 純白
            { "🥦", 1344358873225625600 }, // 向日葵
            { "🥬", 1344359212633034812 }, // 金赤
        };

        public BotService(IConfiguration configuration, ILogger<BotService> logger, CommandService commandService)
        {
            _configuration = configuration;
            _logger = logger;
            _commandService = commandService;
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,  // 使用全部的 Intents
                AlwaysDownloadUsers = true  // 確保用戶資料被下載
            };
            _client = new DiscordSocketClient(config);

            _client.ReactionAdded += OnReactionAdded;
            _client.ReactionRemoved += OnReactionRemoved;
        }

        public async Task StartAsync()
        {
            _client.Log += LogAsync;
            _client.MessageReceived += MessageReceivedAsync;

            string discordBotToken = Environment.GetEnvironmentVariable("DISCORDBOT_TOKEN");

            Debug.WriteLine("discordToken = " + discordBotToken);

            if (string.IsNullOrEmpty(discordBotToken))
            {
                throw new Exception("Discord token is missing!");
            }
            await _client.LoginAsync(TokenType.Bot, discordBotToken);
            await _client.StartAsync();

            // 防止方法立即結束
            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            _logger.LogInformation(log.Message);
            return Task.CompletedTask;
        }

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


        // 發送訊息到指定頻道 ID
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
                Debug.WriteLine("指定的頻道 ID 無效！");
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
                        Debug.WriteLine("訊息刪除成功！");
                    }
                    else
                    {
                        Debug.WriteLine("未找到指定的訊息！");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"刪除訊息時發生錯誤: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("指定的頻道 ID 無效！");
            }
        }

        /// <summary>
        /// 監聽增加表情
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="channel"></param>
        /// <param name="reaction"></param>
        /// <returns></returns>
        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            try
            {
                if (!_messageIds.Contains(reaction.MessageId)) return;

                if (_reactionRoleMap.TryGetValue(reaction.Emote.Name, out ulong roleId))
                {
                    var guild = (reaction.Channel as SocketGuildChannel)?.Guild;
                    var user = guild?.GetUser(reaction.UserId);
                    if (user != null)
                    {
                        var role = guild.GetRole(roleId);
                        if (role != null)
                        {
                            /*
                            // 檢查機器人的權限
                            var bot = guild.CurrentUser;
                            // 診斷資訊
                            Debug.WriteLine("=== 診斷資訊 ===");
                            Debug.WriteLine($"機器人名稱: {bot.Username}");
                            Debug.WriteLine($"機器人最高身分組位階: {bot.Roles.Max(r => r.Position)}");
                            Debug.WriteLine($"目標身分組 '{role?.Name}' 位階: {role?.Position}");
                            Debug.WriteLine($"機器人權限: {string.Join(", ", bot.GuildPermissions.ToList())}");
                            Debug.WriteLine($"機器人的所有身分組: {string.Join(", ", bot.Roles.Select(r => $"{r.Name}({r.Position})"))}");
                            */

                            try
                            {
                                await user.AddRoleAsync(role);
                                Debug.WriteLine($"✅ 已給 {user.Username} 添加身分組 {role.Name}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"添加身分組時發生錯誤: {ex.Message}");
                                Debug.WriteLine($"錯誤詳情: {ex}");
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"表情不對 : " + reaction.Emote.Name);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"處理反應時發生錯誤: {ex.Message}");
                Debug.WriteLine($"錯誤詳情: {ex}");
            }

        }

        /// <summary>
        /// 監聽移除表情
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="channel"></param>
        /// <param name="reaction"></param>
        /// <returns></returns>
        private async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (!_messageIds.Contains(reaction.MessageId)) return;

            if (_reactionRoleMap.TryGetValue(reaction.Emote.Name, out ulong roleId))
            {
                var guild = (reaction.Channel as SocketGuildChannel)?.Guild;
                var user = guild?.GetUser(reaction.UserId);
                if (user != null)
                {
                    var role = guild.GetRole(roleId);
                    if (role != null)
                    {
                        await user.RemoveRoleAsync(role);
                        Debug.WriteLine($"❌ 已移除 {user.Username} 的身分組 {role.Name}");
                    }
                }
            }
        }
    }
}
