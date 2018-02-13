using Rebus.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Abplus.MqMessages.RebusCore.MesssageRetryStrategy
{
    public class NoAutingLoggerTracker : IErrorTracker
    {
        public void CleanUp(string messageId)
        {
        }
        public IEnumerable<Exception> GetExceptions(string messageId)
        {
            return new List<Exception>();
        }
        public string GetFullErrorDescription(string messageId)
        {
            return string.Empty;
        }
        public string GetShortErrorDescription(string messageId)
        {
            return string.Empty;
        }
        public bool HasFailedTooManyTimes(string messageId)
        {
            return false;
        }
        public void RegisterError(string messageId, Exception exception)
        {
        }
    }
}
