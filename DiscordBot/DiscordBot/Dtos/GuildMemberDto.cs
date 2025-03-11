namespace DiscordBot.Dtos
{
    public class GuildMemberDto
    {
        public string MemberName { get; set; } // 伺服器內的暱稱
        public ulong DiscordId { get; set; }   // Discord 使用者 ID
    }
}
