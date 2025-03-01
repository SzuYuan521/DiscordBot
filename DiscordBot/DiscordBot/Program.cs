using DiscordBot.Services;
using OfficeOpenXml;
using Quartz.Impl;
using Quartz.Spi;
using Quartz;
using DiscordBot.Jobs;
using DiscordBot.Models;
using DiscordBot.Data;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// 設定 ExcelPackage 的授權為非商業用途
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// 加入Render端口配置
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// 註冊 PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// 註冊應用程式所需的服務
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);  // 設定檔案管理
builder.Services.AddSingleton<CommandService>(); // 註冊 Discord 機器人指令服務
builder.Services.AddSingleton<BotService>(); // 註冊 Discord Bot 服務
builder.Services.AddControllersWithViews(); // 註冊 MVC Controller 和 View

// 註冊與資料庫相關的服務, 使用 Scoped, 確保在請求範圍內獨立使用
builder.Services.AddScoped<JobScheduleService>(); // 註冊 JobSchedule 管理服務
builder.Services.AddScoped<MessageService>(); // 註冊訊息管理服務

// 註冊 Quartz 任務, 設定為 Transient, 每次執行時都會建立新的執行實例
builder.Services.AddTransient<ExecuteJob>();

// 註冊 Quartz 相關的服務
builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
builder.Services.AddSingleton<IJobFactory, SingletonJobFactory>();

// 註冊 Quartz 託管服務, 確保應用程式啟動時會自動載入 JobSchedules
builder.Services.AddHostedService<QuartzHostedService>();

var app = builder.Build();

// 啟動 Discord Bot, 確保機器人啟動時立即連線
var isBotReady = false;
Task.Run(async () =>
{
    var discordBotService = app.Services.GetRequiredService<BotService>();
    await discordBotService.StartAsync();
    isBotReady = true;
});

/*
// 取得 BotService 服務並啟動 Discord 機器人
var discordBotService = app.Services.GetRequiredService<BotService>();
await discordBotService.StartAsync();*/


// 設定 MVC 路由, 將預設 Controller 設定為 BotController
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Bot}/{action=Index}/{id?}");

app.Run();
