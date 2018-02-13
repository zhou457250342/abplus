﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Abp.Modules;
using Abp.MqMessages.Publishers;
using Rebus.Auditing.Messages;
using Rebus.Bus;
using Rebus.CastleWindsor;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry;
using Abplus.MqMessages.RebusCore.MesssageRetryStrategy;
using Rebus.Retry.Simple;

namespace Abp.MqMessages.Consumers
{
    [DependsOn(typeof(RebusRabbitMqPublisherCoreModule))]
    public class RebusRabbitMqConsumerModule : AbpModule
    {
        private IBus _bus;

        public override void PreInitialize()
        {
            IocManager.Register<IRebusRabbitMqConsumerModuleConfig, RebusRabbitMqConsumerModuleConfig>();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }

        public override void PostInitialize()
        {
            var moduleConfig = IocManager.Resolve<IRebusRabbitMqConsumerModuleConfig>();

            if (moduleConfig.Enabled)
            {
                var rebusConfig = Configure.With(new CastleWindsorContainerAdapter(IocManager.IocContainer));

                if (moduleConfig.LoggingConfigurer != null)
                {
                    rebusConfig.Logging(moduleConfig.LoggingConfigurer);
                }

                rebusConfig.Serialization(moduleConfig.SerializerConfigurer);

                if (moduleConfig.OptionsConfigurer != null)
                {
                    rebusConfig.Options(moduleConfig.OptionsConfigurer);
                }

                rebusConfig.Options(c =>
                {
                    c.SetMaxParallelism(moduleConfig.MaxParallelism);
                    c.SetNumberOfWorkers(moduleConfig.NumberOfWorkers);
                });

                if (moduleConfig.MessageAuditingEnabled)
                {
                    rebusConfig.Options(o => o.EnableMessageAuditing(moduleConfig.MessageAuditingQueueName));
                }
                else
                {
                    rebusConfig.Options(o =>
                    {
                        o.Register<IErrorTracker>(op => new NoAutingLoggerTracker());
                        o.Register<IRetryStrategy>(c =>
                        {
                            var retryStrategySettings = new RetryStrategySettings(c.Get<SimpleRetryStrategySettings>(), true);
                            var errorTracker = c.Get<IErrorTracker>();
                            var errorHandler = c.Get<IErrorHandler>();
                            return new RetryStrategy(retryStrategySettings, errorTracker, errorHandler);
                        });
                    }
                    );
                }

                var mqMessageTypes = new List<Type>();
                //Register handlers first!
                foreach (var assembly in moduleConfig.AssemblysIncludeRebusMqMessageHandlers)
                {
                    IocManager.IocContainer.AutoRegisterHandlersFromAssembly(assembly);

                    mqMessageTypes.AddRange(assembly.GetTypes()
                        .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>)))
                        .SelectMany(t => t.GetInterfaces())
                        .Distinct()
                        .SelectMany(t => t.GetGenericArguments())
                        .Distinct());
                }

                _bus = rebusConfig.Transport(c => c.UseRabbitMq(moduleConfig.ConnectString, moduleConfig.QueueName))
                      .Start();

                //Subscribe messages
                mqMessageTypes = mqMessageTypes.Distinct().ToList();

                foreach (var mqMessageType in mqMessageTypes)
                {
                    _bus.Subscribe(mqMessageType);
                }
            }
        }

        public override void Shutdown()
        {
            if (_bus != null)
            {
                _bus.Dispose();
            }
        }
    }
}
