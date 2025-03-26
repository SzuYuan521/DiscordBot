namespace DiscordBot.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 取出成員遊戲名稱
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ExtractCleanName(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            if (input.Contains("⋆"))
            {
                var parts = input.Split("⋆");
                string beforeStar = parts[0].Trim();
                string afterStar = parts.Length > 1 ? parts[1].Trim() : "";

                if (string.IsNullOrEmpty(afterStar))
                {
                    return beforeStar;
                }

                // 如果 afterStar 是純數字或只有數字+符號, 則回傳 beforeStar
                if (System.Text.RegularExpressions.Regex.IsMatch(afterStar, @"^[\d\s/]+$"))
                {
                    return beforeStar;
                }

                // 如果 afterStar 含文字和數字, 只取前面的文字
                var match = System.Text.RegularExpressions.Regex.Match(afterStar, @"^[^\d/]+");
                if (match.Success)
                {
                    return match.Value.Trim();
                }

                // 其他情況就直接回傳 afterStar
                return afterStar;
            }

            // 沒有 ⋆ 就回傳原本
            return input.Trim();
        }
    }

}
