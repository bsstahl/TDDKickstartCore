using LiquidNun.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using QueueProfiler.Data;

namespace QueueProfiler
{
    public class Queue
    {
        IReliableMessaging _messagingProvider;
        ITimingProvider _timingProvider;
        IQueueDepthRepository _dataProvider;

        string _queueName;

        public Queue(IServiceProvider serviceProvider, string queueName)
        {
            _messagingProvider = serviceProvider.GetService<IReliableMessaging>();
            _timingProvider = serviceProvider.GetService<ITimingProvider>();
            _dataProvider = serviceProvider.GetService<IQueueDepthRepository>();
            _queueName = queueName;
        }

        public void Process(int numberOfExecutions, TimeSpan delayBetweenExecutions)
        {
            for (int i = 0; i < numberOfExecutions; i++)
            {
                var depth = _messagingProvider.GetDepth(_queueName);
                _dataProvider.Save(_queueName, DateTime.UtcNow, depth);
                if ((i+1) < numberOfExecutions)
                    _timingProvider.Delay(delayBetweenExecutions);
            }
        }
    }
}
