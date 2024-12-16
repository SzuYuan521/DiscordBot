using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        public BotService(IConfiguration configuration, ILogger<BotService> logger, CommandService commandService)
        {
            _configuration = configuration;
            _logger = logger;
            _commandService = commandService;
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds |
                     GatewayIntents.GuildMessages |
                     GatewayIntents.MessageContent
            };
            _client = new DiscordSocketClient(config);
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
                Console.WriteLine("指定的頻道 ID 無效！");
            }
        }
    }
}
