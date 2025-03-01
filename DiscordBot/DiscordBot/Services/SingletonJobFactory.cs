using Quartz;
using Quartz.Spi;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DiscordBot.Services
{
    public class SingletonJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SingletonJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            // 直接返回 ExecuteJob，它會在 Execute 方法內自己創建 scope
            return _serviceProvider.GetRequiredService(bundle.JobDetail.JobType) as IJob;
        }

        public void ReturnJob(IJob job)
        {
            // 不需要做特別處理
            (job as IDisposable)?.Dispose();
        }
    }
}
