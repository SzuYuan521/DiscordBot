using DiscordBot.Data;
using DiscordBot.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Services
{
    public class GuildMemberService
    {
        private readonly ApplicationDbContext _dbContext;

        public GuildMemberService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 取得所有幫會成員
        /// </summary>
        public async Task<List<GuildMember>> GetAllMembersAsync()
        {
            return await _dbContext.GuildMembers.ToListAsync();
        }

        /// <summary>
        /// 根據 Discord ID 取得幫會成員
        /// </summary>
        public async Task<GuildMember?> GetMemberByDiscordIdAsync(long discordId)
        {
            return await _dbContext.GuildMembers.FirstOrDefaultAsync(m => m.DiscordId == discordId);
        }

        /// <summary>
        /// 新增幫會成員
        /// </summary>
        public async Task AddMemberAsync(GuildMember member)
        {
            await _dbContext.GuildMembers.AddAsync(member);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 更新幫會成員資訊
        /// </summary>
        public async Task UpdateMemberAsync(GuildMember member)
        {
            _dbContext.GuildMembers.Update(member);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 刪除幫會成員
        /// </summary>
        public async Task DeleteMemberAsync(long discordId)
        {
            var member = await _dbContext.GuildMembers.FirstOrDefaultAsync(m => m.DiscordId == discordId);
            if (member != null)
            {
                _dbContext.GuildMembers.Remove(member);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}