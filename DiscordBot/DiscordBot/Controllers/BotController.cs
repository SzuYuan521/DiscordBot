using Microsoft.AspNetCore.Mvc;
using DiscordBot.Services;

namespace DiscordBot.Controllers
{
    public class BotController : Controller
    {
        private readonly CommandService _commandService;

        public BotController(CommandService commandService)
        {
            _commandService = commandService;
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
    }
}
