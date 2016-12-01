using Akka.Configuration;
using Akka.TestKit;

namespace Divverence.MarbleTesting.Akka.Async
{
    public static class ConfigExtensionsForAsync
    {
        public static Config AsyncTestingConfig => TestKitBase.DefaultConfig.WithAsyncTestDispatcher();

        public static Config AsyncTestDispatcherConfig
            => ConfigurationFactory.ParseString(AsyncTestDispatcherConfigHocon);

        public static Config WithAsyncTestDispatcher(this Config baseConfig)
            => AsyncTestDispatcherConfig.WithFallback(baseConfig);

        public static string AsyncTestDispatcherConfigHocon { get; } =
            $@"akka.actor.default-dispatcher.type = ""{typeof(AwaitableTaskDispatcherConfigurator).AssemblyQualifiedName}""
akka.test.test-actor.dispatcher.type = ""{typeof(AwaitableTaskDispatcherConfigurator).AssemblyQualifiedName}""";
    }
}