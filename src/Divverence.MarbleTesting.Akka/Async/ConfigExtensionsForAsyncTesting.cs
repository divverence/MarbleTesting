using Akka.Configuration;
using Akka.TestKit;

namespace Divverence.MarbleTesting.Akka.Async
{
    public static class ConfigExtensionsForAsyncTesting
    {
        public static Config AsyncTestingConfig
            => TestKitBase.DefaultConfig.WithAwaitableTaskDispatcher().WithTestScheduler();

        #region AwaitableDispatcher

        public static Config WithAwaitableTaskDispatcher(this Config baseConfig)
            => AwaitableTaskDispatcherConfig.WithFallback(baseConfig);

        public static Config AwaitableTaskDispatcherConfig
            => ConfigurationFactory.ParseString(AwaitableTaskDispatcherConfigHocon);

        public static string AwaitableTaskDispatcherConfigHocon { get; } =
            $@"akka.actor.default-dispatcher.type = ""{typeof(AwaitableTaskDispatcherConfigurator).AssemblyQualifiedName}""
akka.test.test-actor.dispatcher.type = ""{typeof(AwaitableTaskDispatcherConfigurator).AssemblyQualifiedName}""";

        #endregion

        #region TestScheduler

        public static Config WithTestScheduler(this Config baseConfig)
            => AwaitableTaskDispatcherConfig.WithFallback(baseConfig);

        public static Config TestSchedulerConfig
            => ConfigurationFactory.ParseString(TestSchedulerHocon);

        public static string TestSchedulerHocon
            => $@"akka.scheduler.implementation = ""{typeof(TestScheduler).AssemblyQualifiedName}""";

        #endregion
    }
}