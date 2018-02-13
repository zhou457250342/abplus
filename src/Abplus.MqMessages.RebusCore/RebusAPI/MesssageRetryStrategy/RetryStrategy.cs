using Rebus.Retry;
using Rebus.Retry.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Abplus.MqMessages.RebusCore.MesssageRetryStrategy
{
    public class RetryStrategy : IRetryStrategy
    {
        readonly RetryStrategySettings _retryStrategySettings;
        readonly IErrorTracker _errorTrackert;
        readonly IErrorHandler _errorHandlert;

        public RetryStrategy(RetryStrategySettings retryStrategySettings,
            IErrorTracker errorTracker,
            IErrorHandler errorHandler)
        {
            _retryStrategySettings = retryStrategySettings ?? throw new ArgumentNullException(nameof(retryStrategySettings));
            _errorTrackert = errorTracker ?? throw new ArgumentNullException(nameof(errorTracker));
            _errorHandlert = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }
        public IRetryStrategyStep GetRetryStep()
        {
            return new RetryStrategyStep(_retryStrategySettings, _errorTrackert, _errorHandlert);
        }
    }
}
