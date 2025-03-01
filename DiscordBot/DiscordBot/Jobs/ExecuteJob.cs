using Discord;
using Quartz;
using DiscordBot.Services;
using DiscordBot.Enums;
using DiscordBot.Models;
using System.Diagnostics;

namespace DiscordBot.Jobs
{
    /// <summary>
    /// Quartz 任務執行類別, 根據排程類型執行相對應的動作
    /// </summary>
    public class ExecuteJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;

        public ExecuteJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Quartz 任務執行邏輯, 從 JobDataMap 取得 JobId, 並根據類型執行對應操作。
        /// </summary>
        /// <param name="context">Quartz 任務執行上下文</param>
        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;
            int jobId = dataMap.GetInt("JobId");

            Console.WriteLine($"[Quartz] 正在執行 Job Id: {jobId}");

            // 建立 Scoped, 確保不影響 Singleton
            using (var scope = _serviceProvider.CreateScope())
            {
                var jobScheduleService = scope.ServiceProvider.GetRequiredService<JobScheduleService>();
                var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();
                var botService = scope.ServiceProvider.GetRequiredService<BotService>();

                // 取得該 Job 的排程資訊
                var jobSchedule = await jobScheduleService.GetJobScheduleByIdAsync(jobId);
                if (jobSchedule == null || !jobSchedule.Enabled)
                {
                    Console.WriteLine($"[Quartz] ⚠️ Job {jobId} 未啟用或不存在, 跳過執行");
                    return;
                }

                // 根據不同的 Job 類型執行相對應的操作
                switch (jobSchedule.JobType)
                {
                    case JobType.SendMessage:
                        await SendMessage(jobSchedule, messageService, botService);
                        break;

                    case JobType.RunCommand:
                        await RunCommand(jobSchedule);
                        break;

                    case JobType.ClearData:
                        await ClearData(jobSchedule);
                        break;

                    default:
                        Console.WriteLine($"[Error] 未知的 JobType: {jobSchedule.JobType}");
                        break;
                }
            } // Scoped 服務 (DbContext) 會在此處正確釋放
        }

        /// <summary>
        /// 發送訊息的任務執行邏輯, 從資料庫取得訊息內容並發送至 Discord 頻道
        /// </summary>
        /// <param name="job">Job 排程資訊</param>
        /// <param name="messageService">訊息服務</param>
        /// <param name="botService">Discord Bot 服務</param>
        private async Task SendMessage(JobSchedule job, MessageService messageService, BotService botService)
        {
            if (job.MessageId.HasValue)
            {
                var message = await messageService.GetMessageByIdAsync(job.MessageId.Value);
                var channel = botService.GetClient().GetChannel(message.ChannelId) as IMessageChannel;
                if (channel != null)
                {
                    await channel.SendMessageAsync(message.Content);
                }
            }
        }

        /// <summary>
        /// 執行指令的任務執行邏輯, 目前無實做
        /// </summary>
        /// <param name="job">Job 排程資訊</param>
        private async Task RunCommand(JobSchedule job)
        {
            Console.WriteLine($"[執行指令] {job.Command}");
            // 之後再實作指令執行
        }

        /// <summary>
        /// 清除資料的任務執行邏輯, 目前無實做
        /// </summary>
        /// <param name="job">Job 排程資訊</param>
        private async Task ClearData(JobSchedule job)
        {
            Console.WriteLine($"[刪除資料] {job.Id}");
            // 之後再實作刪除資料
        }
    }
}
