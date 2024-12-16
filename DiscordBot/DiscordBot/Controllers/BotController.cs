using Microsoft.AspNetCore.Mvc;
using DiscordBot.Services;

namespace DiscordBot.Controllers
{
    public class BotController : Controller
    {
        private readonly CommandService _commandService;
        private readonly BotService _botService;

        public BotController(CommandService commandService, BotService botService)
        {
            _commandService = commandService;
            _botService = botService;
        }

        // 顯示查詢指令頁面
        public IActionResult Index()
        {
            return View();
        }

        // 查詢指令回應的API
        [HttpPost]
        public async Task<IActionResult> ExecuteCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                ViewBag.Message = "請輸入有效的指令！";
                return View("Index");
            }

            var response = await _commandService.GetResponse(command);

            if (!string.IsNullOrEmpty(response))
            {
                ViewBag.Message = response;
            }
            else
            {
                ViewBag.Message = "未找到對應的指令";
            }

            return View("Index");
        }

        // 發送莊園高價收購的文字到指定頻道
        [HttpPost]
        public async Task<IActionResult> ManorAcquisitionAtHighPrice(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                ViewBag.Message = "請輸入有效的文字！";
                return View("Index");
            }

            // 發送訊息到指定頻道(墨雨海棠-莊園收購與翻土區)
            ulong channelId = 1316940662838661190;
            await _botService.SendMessageToChannel(channelId, text);

            ViewBag.Message = "訊息已成功發送到頻道！";

            return View("Index");
        }
    }
}
