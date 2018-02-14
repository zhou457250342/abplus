using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Exceptions;

namespace Abplus.MqMessages.RebusCore.MesssageRetryStrategy
{
    [StepDocumentation(@"Wraps the invocation of the entire receive pipeline in an exception handler, tracking the number of times the received message has been attempted to be delivered.
If the maximum number of delivery attempts is reached, the message is moved to the error queue.")]
    public class RetryStrategyStep : IRetryStrategyStep
    {
        public const string DispatchAsFailedMessageKey = "dispatch-as-failed-message";

        readonly RetryStrategySettings _retryStrategySettings;
        readonly SimpleRetryStrategySettings _simpleRetryStrategySettings;
        readonly IErrorTracker _errorTracker;
        readonly IErrorHandler _errorHandler;

        public RetryStrategyStep(RetryStrategySettings retryStrategySettings,
            IErrorTracker errorTracker,
            IErrorHandler errorHandler)
        {
            _retryStrategySettings = retryStrategySettings ?? throw new ArgumentNullException(nameof(retryStrategySettings));
            _simpleRetryStrategySettings = retryStrategySettings.SimpleRetryStrategySettings ?? throw new ArgumentNullException(nameof(retryStrategySettings.SimpleRetryStrategySettings));
            _errorTracker = errorTracker ?? throw new ArgumentNullException(nameof(errorTracker));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }


        /// <summary>
        /// Executes the entire message processing pipeline in an exception handler, tracking the number of failed delivery attempts.
        /// Forwards the message to the error queue when the max number of delivery attempts has been exceeded.
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transportMessage = context.Load<TransportMessage>();
            var transactionContext = context.Load<ITransactionContext>();
            var messageId = transportMessage.Headers.GetValueOrNull(Headers.MessageId);

            if (string.IsNullOrWhiteSpace(messageId))
            {
                await MoveMessageToErrorQueue(context.Load<OriginalTransportMessage>(), transactionContext,
                    new RebusApplicationException($"Received message with empty or absent '{Headers.MessageId}' header! All messages must be" +
                                                  " supplied with an ID . If no ID is present, the message cannot be tracked" +
                                                  " between delivery attempts, and other stuff would also be much harder to" +
                                                  " do - therefore, it is a requirement that messages be supplied with an ID."));

                return;
            }

            if (_errorTracker.HasFailedTooManyTimes(messageId))
            {
                // if we don't have 2nd level retries, just get the message out of the way
                if (!_simpleRetryStrategySettings.SecondLevelRetriesEnabled)
                {
                    var aggregateException = GetAggregateException(messageId);
                    await MoveMessageToErrorQueue(context.Load<OriginalTransportMessage>(), transactionContext, aggregateException);
                    _errorTracker.CleanUp(messageId);
                    return;
                }

                // change the identifier to track by to perform this 2nd level of delivery attempts
                var secondLevelMessageId = GetSecondLevelMessageId(messageId);

                if (_errorTracker.HasFailedTooManyTimes(secondLevelMessageId))
                {
                    var aggregateException = GetAggregateException(messageId, secondLevelMessageId);
                    await MoveMessageToErrorQueue(context.Load<OriginalTransportMessage>(), transactionContext, aggregateException);
                    _errorTracker.CleanUp(messageId);
                    _errorTracker.CleanUp(secondLevelMessageId);
                    return;
                }

                context.Save(DispatchAsFailedMessageKey, true);

                await DispatchWithTrackerIdentifier(next, secondLevelMessageId, transactionContext, new[] { messageId, secondLevelMessageId });

                return;
            }

            await DispatchWithTrackerIdentifier(next, messageId, transactionContext, new[] { messageId });
        }

        AggregateException GetAggregateException(params string[] ids)
        {
            var exceptions = ids.SelectMany(_errorTracker.GetExceptions).ToArray();

            return new AggregateException($"{exceptions.Length} unhandled exceptions", exceptions);
        }

        static string GetSecondLevelMessageId(string messageId)
        {
            return messageId + "-2nd-level";
        }

        async Task DispatchWithTrackerIdentifier(Func<Task> next, string identifierToTrackMessageBy, ITransactionContext transactionContext, string[] identifiersToClearOnSuccess)
        {
            try
            {
                await next();

                await transactionContext.Commit();

                foreach (var id in identifiersToClearOnSuccess)
                {
                    _errorTracker.CleanUp(id);
                }
            }
            catch (Exception exception)
            {
                _errorTracker.RegisterError(identifierToTrackMessageBy, exception);

                transactionContext.Abort();
            }
        }

        async Task MoveMessageToErrorQueue(OriginalTransportMessage originalTransportMessage, ITransactionContext transactionContext, Exception exception)
        {
            var transportMessage = originalTransportMessage.TransportMessage;

            await _errorHandler.HandlePoisonMessage(transportMessage, transactionContext, exception);
        }
    }
}
