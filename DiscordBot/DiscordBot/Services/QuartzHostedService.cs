using DiscordBot.Services;
using Quartz;
using Quartz.Spi;
using DiscordBot.Jobs;
using System.Diagnostics;

/// <summary>
/// Quartz 託管服務, 負責管理任務排程的啟動與停止
/// </summary>
public class QuartzHostedService : IHostedService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobFactory _jobFactory;
    private readonly IServiceProvider _serviceProvider;
    private IScheduler _scheduler;

    /// <summary>
    /// 注入ServiceProvider, SchedulerFactory, JobFactory
    /// </summary>
    /// <param name="serviceProvider">用於解析 Scoped Service</param>
    /// <param name="schedulerFactory">Quartz 的 SchedulerFactory</param>
    /// <param name="jobFactory">Quartz 的 JobFactory</param>
    public QuartzHostedService(
        IServiceProvider serviceProvider,
        ISchedulerFactory schedulerFactory,
        IJobFactory jobFactory)
    {
        _serviceProvider = serviceProvider;
        _schedulerFactory = schedulerFactory;
        _jobFactory = jobFactory;
    }

    /// <summary>
    /// 啟動 Quartz 排程器, 並載入所有啟用的排程
    /// </summary>
    /// <param name="cancellationToken">取消標記</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 建立 Quartz 排程器實例
        _scheduler = await _schedulerFactory.GetScheduler();
        _scheduler.JobFactory = _jobFactory;

        // 使用 Scope 確保 Scoped 服務不影響 Singleton
        using (var scope = _serviceProvider.CreateScope())
        {
            var jobScheduleService = scope.ServiceProvider.GetRequiredService<JobScheduleService>();
            var jobSchedules = await jobScheduleService.GetEnabledSchedulesAsync();

            // 將所有啟用的排程加入 Quartz
            foreach (var jobSchedule in jobSchedules)
            {
                var jobKey = new JobKey($"Job_{jobSchedule.Id}_{jobSchedule.JobType}");
                var triggerKey = new TriggerKey($"Trigger_{jobSchedule.Id}_{jobSchedule.JobType}");

                // 檢查是否已經存在相同的 Job, 避免重複排程
                if (!await _scheduler.CheckExists(jobKey, cancellationToken))
                {
                    // 建立 Job 實例並存入 Job ID
                    var jobDetail = JobBuilder.Create<ExecuteJob>()
                        .WithIdentity(jobKey)
                        .UsingJobData("JobId", jobSchedule.Id)
                        .Build();

                    // 建立觸發條件, 依據 Cron 表達式執行
                    var trigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .WithCronSchedule(jobSchedule.CronExpression, x => x
                            .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"))) // 設定時區是台北
                        .Build();

                    // 註冊排程
                    await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
                    Debug.WriteLine($"[Quartz] 已註冊 Job: {jobSchedule.JobType} (ID: {jobSchedule.Id})");
                }
                else
                {
                    Debug.WriteLine($"[Quartz] Job 已存在: {jobSchedule.JobType} (ID: {jobSchedule.Id}), 跳過");
                }
            }
        }

        // 啟動 Quartz 排程器
        await _scheduler.Start(cancellationToken);
    }

    /// <summary>
    /// 停止 Quartz 排程器
    /// </summary>
    /// <param name="cancellationToken">取消標記</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
            Debug.WriteLine("[Quartz] 任務排程已停止");
        }
    }
}
