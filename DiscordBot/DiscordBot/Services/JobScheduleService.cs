using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DiscordBot.Data;
using DiscordBot.Models;
using DiscordBot.Extensions;

namespace DiscordBot.Services
{
    /// <summary>
    /// 對 JobSchedule table 的存取與管理
    /// </summary>
    public class JobScheduleService 
    {
        private readonly ApplicationDbContext _dbContext;

        public JobScheduleService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 根據 Job Id 取得對應的 JobSchedule 記錄
        /// </summary>
        /// <param name="id">JobSchedule 的 Id</param>
        /// <returns>回傳對應的 JobSchedule, 若不存在則回傳 null</returns>
        public async Task<JobSchedule?> GetJobScheduleByIdAsync(int id)
        {
            return await _dbContext.JobSchedules.FindAsync(id);
        }

        /// <summary>
        /// 取得所有 Enabled 的 JobSchedule 
        /// </summary>
        /// <returns>回傳所有啟用中的 JobSchedule</returns>
        public async Task<List<JobSchedule>> GetEnabledSchedulesAsync()
        {
            return await _dbContext.JobSchedules.Where(j => j.Enabled).ToListAsync();
        }

        /// <summary>
        /// 新增一筆 JobSchedule
        /// </summary>
        /// <param name="jobSchedule">要新增的 JobSchedule 物件</param>
        public async Task AddJobScheduleAsync(JobSchedule jobSchedule)
        {
            await _dbContext.JobSchedules.AddAsync(jobSchedule);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 更新現有的 JobSchedule
        /// </summary>
        /// <param name="jobSchedule">要更新的 JobSchedule 物件</param>
        public async Task UpdateJobScheduleAsync(JobSchedule jobSchedule)
        {
            jobSchedule.UpdatedAt = DateTime.UtcNow;
            _dbContext.JobSchedules.Update(jobSchedule);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 根據 Id 刪除指定的 JobSchedule
        /// </summary>
        /// <param name="id">JobSchedule 的 Id</param>
        public async Task DeleteJobScheduleAsync(int id)
        {
            var job = await _dbContext.JobSchedules.FindAsync(id);
            if (job != null)
            {
                _dbContext.JobSchedules.Remove(job);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
