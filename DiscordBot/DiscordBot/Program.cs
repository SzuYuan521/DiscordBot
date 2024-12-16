using DiscordBot.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);



builder.Services.AddSingleton<CommandService>();
builder.Services.AddSingleton<BotService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

var discordBotService = app.Services.GetRequiredService<BotService>();
Task.Run(() => discordBotService.StartAsync());  // 放入 Task.Run 以讓其在背景執行
//await discordBotService.StartAsync();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Bot}/{action=Index}/{id?}");

app.Run();
