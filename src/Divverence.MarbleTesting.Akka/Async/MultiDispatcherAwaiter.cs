using System;
using System.Threading.Tasks;
using Akka.Actor;

namespace Divverence.MarbleTesting.Akka.Async
{
    public class MultiDispatcherAwaiter : IWaitForIdleOrActive
    {
        private readonly AwaitableTaskDispatcher[] _dispatchers;

        public MultiDispatcherAwaiter(params AwaitableTaskDispatcher[] dispatchers)
        {
            _dispatchers = dispatchers;
        }

        public Task Active()
        {
            return AwaitableTaskDispatcher.NotIdle(_dispatchers);
        }

        public Task Idle()
        {
            return AwaitableTaskDispatcher.Idle(_dispatchers);
        }

        public static MultiDispatcherAwaiter CreateFromActorSystem(ActorSystem actorSystem)
        {
            var testDispatcher = actorSystem.Dispatchers.DefaultGlobalDispatcher as AwaitableTaskDispatcher;
            if (testDispatcher == null)
                throw new InvalidOperationException(
                    $"akka.actor.default-dispatcher.type must be configured to \"{typeof(AwaitableTaskDispatcher).AssemblyQualifiedName}\"");

            var testActorDispatcher =
                actorSystem.Dispatchers.Lookup("akka.test.test-actor.dispatcher") as AwaitableTaskDispatcher;
            if (testActorDispatcher == null)
                throw new InvalidOperationException(
                    $"akka.test.test-actor.dispatcher must be configured to \"{typeof(AwaitableTaskDispatcher).AssemblyQualifiedName}\"");

            return new MultiDispatcherAwaiter(testDispatcher, testActorDispatcher);
        }
    }
}