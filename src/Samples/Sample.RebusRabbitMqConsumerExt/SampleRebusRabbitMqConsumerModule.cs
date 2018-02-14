#if NET462
using Abp.Modules;
using Abp.MqMessages.Consumers;
using Abp.Configuration.Startup;
using System.Reflection;
using Rebus.NLog.Config;
using Castle.Facilities.Logging;

namespace Sample
{
    [DependsOn(typeof(RebusRabbitMqConsumerModule))]
    public class SampleRebusRabbitMqConsumerModule : AbpModule
    {
        public override void PreInitialize()
        {

            Configuration.Modules.UseAbplusRebusRabbitMqConsumer()
                .UseLogging(c => c.NLog())
                .ConnectTo("amqp://flymax:123456@localhost:5672")
                .UseQueue(Assembly.GetExecutingAssembly().GetName().Name)
                .RegisterHandlerInAssemblys(Assembly.GetExecutingAssembly());
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void PostInitialize()
        {
            Abp.Dependency.IocManager.Instance.IocContainer.AddFacility<LoggingFacility>(f => f.UseNLog().WithConfig("nlog.config"));
        }
    }
}
#endif
