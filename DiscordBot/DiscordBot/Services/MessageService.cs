using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DiscordBot.Data;
using DiscordBot.Models;

namespace DiscordBot.Services
{
    /// <summary>
    /// 對 Discord 訊息的管理
    /// </summary>
    public class MessageService
    {
        private readonly ApplicationDbContext _dbContext;

        public MessageService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 根據訊息 Id 取得對應的 Discord 訊息
        /// </summary>
        /// <param name="id">訊息的唯一識別碼</param>
        /// <returns>回傳 DiscordMessage 物件, 若不存在則回傳 null</returns>
        public async Task<DiscordMessage?> GetMessageByIdAsync(int id)
        {
            return await _dbContext.DiscordMessages.FindAsync(id);
        }

        /// <summary>
        /// 新增一則 Discord 訊息到資料庫
        /// </summary>
        /// <param name="message">要新增的 Discord 訊息物件</param>
        public async Task AddMessageAsync(DiscordMessage message)
        {
            await _dbContext.DiscordMessages.AddAsync(message);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 根據 Id 刪除指定的 Discord 訊息
        /// </summary>
        /// <param name="id">訊息的唯一識別碼</param>
        public async Task DeleteMessageAsync(int id)
        {
            var message = await _dbContext.DiscordMessages.FindAsync(id);
            if (message != null)
            {
                _dbContext.DiscordMessages.Remove(message);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
