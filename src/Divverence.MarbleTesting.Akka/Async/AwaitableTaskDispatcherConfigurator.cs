using Akka.Configuration;
using Akka.Dispatch;

namespace Divverence.MarbleTesting.Akka.Async
{
    public class AwaitableTaskDispatcherConfigurator : MessageDispatcherConfigurator
    {
        public AwaitableTaskDispatcherConfigurator(Config config, IDispatcherPrerequisites prerequisites) : base(config, prerequisites)
        {
        }

        private AwaitableTaskDispatcher _instance;

        public override MessageDispatcher Dispatcher()
        {
            return _instance = _instance ?? new AwaitableTaskDispatcher(this);
        }
    }
}