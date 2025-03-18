using DiscordBot.Data;
using DiscordBot.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Services
{
    public class OneLineBondService
    {
        private readonly ApplicationDbContext _dbContext;

        public OneLineBondService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 取得所有一線牽關係
        /// </summary>
        public async Task<List<OneLineBond>> GetAllBondsAsync()
        {
            return await _dbContext.OneLineBonds.Include(b => b.Member).ToListAsync();
        }

        /// <summary>
        /// 根據會員 ID 取得一線牽關係
        /// </summary>
        public async Task<List<OneLineBond>> GetBondsByMemberIdAsync(long discordId)
        {
            return await _dbContext.OneLineBonds
                .Where(b => b.DiscordId == discordId)
                .Include(b => b.Member)
                .ToListAsync();
        }

        /// <summary>
        /// 新增一條一線牽關係
        /// </summary>
        public async Task AddBondAsync(OneLineBond bond)
        {
            await _dbContext.OneLineBonds.AddAsync(bond);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 更新一線牽關係
        /// </summary>
        public async Task UpdateBondAsync(OneLineBond bond)
        {
            _dbContext.OneLineBonds.Update(bond);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 刪除一條一線牽關係
        /// </summary>
        public async Task DeleteBondAsync(int bondId)
        {
            var bond = await _dbContext.OneLineBonds.FindAsync(bondId);
            if (bond != null)
            {
                _dbContext.OneLineBonds.Remove(bond);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
