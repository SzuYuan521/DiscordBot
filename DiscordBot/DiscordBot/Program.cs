using DiscordBot.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// �]�w EPPlus �����v�W�U�嬰�D�ӷ~�γ~
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);


// ���U�A��
builder.Services.AddSingleton<CommandService>();
builder.Services.AddSingleton<BotService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// �Ұ� Discord Bot
var discordBotService = app.Services.GetRequiredService<BotService>();
await discordBotService.StartAsync();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Bot}/{action=Index}/{id?}");
app.Run();
