using DiscordBot.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// 設定 ExcelPackage 的授權為非商業用途
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// 加入Render端口配置
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<CommandService>();
builder.Services.AddSingleton<BotService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 取得 BotService 服務並啟動 Discord 機器人
var discordBotService = app.Services.GetRequiredService<BotService>();
await discordBotService.StartAsync();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Bot}/{action=Index}/{id?}");
app.Run();
