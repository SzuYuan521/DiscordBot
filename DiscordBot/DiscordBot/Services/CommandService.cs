using OfficeOpenXml;
using System.IO;

namespace DiscordBot.Services
{
    public class CommandService
    {
        private readonly string _filePath;

        public CommandService()
        {
            _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Command.xlsx");
        }

        // 根據指令查詢並返回回應
        public string GetResponse(string command)
        {
            command = command.Trim().ToLower();

            string sheetName = command.StartsWith("/PVE") ? "PVE" : "PVP";
            string response = null;

            using (var package = new ExcelPackage(new FileInfo(_filePath)))
            {
                var sheet = package.Workbook.Worksheets[sheetName];

                if (sheet != null)
                {
                    var rowCount = sheet.Dimension.Rows;
                    for (int row = 1; row <= rowCount; row++)
                    {
                        var commandName = sheet.Cells[row, 1].Text.Trim().ToLower();
                        if (command == commandName)
                        {
                            response = sheet.Cells[row, 2].Text.Trim();
                            break;
                        }
                    }
                }
            }

            return response;
        }
    }
}
