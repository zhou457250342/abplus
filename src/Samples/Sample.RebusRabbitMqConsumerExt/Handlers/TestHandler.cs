﻿#if NET462
using System.Threading.Tasks;
using Castle.Core.Logging;
using Rebus.Handlers;
using Sample.MqMessages;
using Abp.MqMessages;

namespace Sample.Handlers
{
    public class TestHandler : IHandleMessages<TestMqMessage>
    {
        public ILogger Logger { get; set; }
        public IMqMessagePublisher Publisher { get; set; }
        public TestHandler()
        {
            Publisher = NullMqMessagePublisher.Instance;
        }


        public async Task Handle(TestMqMessage message)
        {
            throw new System.Exception("");
            var msg = $"{Logger.GetType()}:{message.Name},{message.Value},{message.Time}";
            Logger.Debug(msg);
            await Publisher.PublishAsync(msg);//send it again!
        }
    }
}
#endif