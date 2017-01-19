using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QueueProfiler.Data
{
    public interface IQueueDepthRepository
    {
        void Save(string queueName, DateTime currentDateTimeUtc, long depth);
    }
}
