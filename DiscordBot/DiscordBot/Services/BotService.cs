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

            var token = _configuration["DiscordBot:Token"];
            await _client.LoginAsync(TokenType.Bot, token);
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
    }
}
