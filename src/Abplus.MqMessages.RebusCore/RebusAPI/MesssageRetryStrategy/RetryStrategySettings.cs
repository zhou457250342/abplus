using Rebus.Retry.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Abplus.MqMessages.RebusCore.MesssageRetryStrategy
{
    public class RetryStrategySettings
    {
        public SimpleRetryStrategySettings SimpleRetryStrategySettings;
        /// <summary>
        /// 日志处理后是否继续消费
        /// </summary>
        public bool UnErrorLogBlockWait { get; set; }
        public RetryStrategySettings(SimpleRetryStrategySettings simple, bool unErrorLogBlockWait = false)
        {
            SimpleRetryStrategySettings = simple ?? throw new ArgumentNullException(nameof(simple));
            UnErrorLogBlockWait = unErrorLogBlockWait;
        }
    }
}
