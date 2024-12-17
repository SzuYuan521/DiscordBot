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

        /// <summary>
        /// 刪除訊息
        /// </summary>
        /// <param name="channelId">頻道ID</param>
        /// <param name="messageId">訊息ID</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DeleteMessage(ulong channelId, ulong messageId)
        {
            if (channelId == 0 || messageId == 0)
            {
                ViewBag.Message = "請輸入有效的頻道 ID 和訊息 ID！";
                return View("Index");
            }

            try
            {
                await _botService.DeleteMessageFromChannel(channelId, messageId);
                ViewBag.Message = "訊息已成功刪除！";
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"刪除訊息時發生錯誤: {ex.Message}";
            }

            return View("Index");
        }
    }
}
