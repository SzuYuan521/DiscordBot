using DiscordBot.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// 設定 ExcelPackage 的授權為非商業用途
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// 加入Render端口配置
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
Console.WriteLine($"Using port: {port}");
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<CommandService>();
builder.Services.AddSingleton<BotService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 加入健康檢查路由
app.MapGet("/", () => "App is running.");

// 啟動 Discord Bot
var isBotReady = false;

Task.Run(async () =>
{
    var discordBotService = app.Services.GetRequiredService<BotService>();
    await discordBotService.StartAsync();
    isBotReady = true;
});

app.MapGet("/health", () => isBotReady ? Results.Ok("Bot is ready.") : Results.StatusCode(503));



/*
// 取得 BotService 服務並啟動 Discord 機器人
var discordBotService = app.Services.GetRequiredService<BotService>();
await discordBotService.StartAsync();*/

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Bot}/{action=Index}/{id?}");

app.Run();
