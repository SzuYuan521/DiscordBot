using OfficeOpenXml;
using System.Data;
using System.IO;

namespace DiscordBot.Services
{
    public class CommandService
    {
        private readonly string _filePath;
        private readonly Dictionary<string, string> _commandTypes;

        public CommandService()
        {
            _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Commands.xlsx");

            // 定義所有可能的指令類型及其對應的工作表名稱
            _commandTypes = new Dictionary<string, string>
            {
                { "/pve", "PVE" },
                { "/pvp", "PVP" },
                { "/hotactivity", "HotActivity" }
            };
        }

        // 根據指令查詢並返回回應
        public async Task<string> GetResponse(string command)
        {
            command = command.Trim().ToLower();

            string sheetName = string.Empty;

            // 遍歷所有可能的指令前綴，查找是否匹配
            foreach (var commandType in _commandTypes)
            {
                if (command.StartsWith(commandType.Key))
                {
                    sheetName = commandType.Value;
                    command = command.Replace(commandType.Key, "").Trim(); // 去掉指令前綴
                    break;
                }
            }

            string response = string.Empty;

            if (sheetName != null)
            {
                using (var package = new ExcelPackage(new FileInfo(_filePath)))
                {
                    var sheet = package.Workbook.Worksheets[sheetName];

                    if (sheet != null)
                    {
                        response = await SearchCommand(sheet, command);
                    }
                    else
                    {
                        response = "找不到表單";
                    }
                }
            }

            return response;
        }

        // 搜尋指定表中的指令
        private async Task<string> SearchCommand(ExcelWorksheet sheet, string command)
        {
            var rowCount = sheet.Dimension.Rows;
            for (int row = 1; row <= rowCount; row++)
            {
                var commandName = sheet.Cells[row, 1].Text.Trim().ToLower();
                if (command == commandName)
                {
                    return sheet.Cells[row, 2].Text.Trim();
                }
            }
            return string.Empty;
        }
    }
}
