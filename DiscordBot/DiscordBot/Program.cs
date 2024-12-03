using DiscordBot.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// 設定 EPPlus 的授權上下文為非商業用途
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);


// 註冊服務
builder.Services.AddSingleton<CommandService>();
builder.Services.AddSingleton<BotService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 啟動 Discord Bot
var discordBotService = app.Services.GetRequiredService<BotService>();
await discordBotService.StartAsync();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Bot}/{action=Index}/{id?}");
app.Run();
