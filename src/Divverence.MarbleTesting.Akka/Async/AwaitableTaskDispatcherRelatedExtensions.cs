using System.Threading.Tasks;
using Akka.Actor;

namespace Divverence.MarbleTesting.Akka.Async
{
    public static class AwaitableTaskDispatcherRelatedExtensions
    {
        public static Task TrackOnDispatcher(this Task task, AwaitableTaskDispatcher dispatcherOrNull)
        {
            dispatcherOrNull?.PoolTask(task);
            return task;
        }

        public static Task TrackOnDispatcher(this Task task, IActorContext context) => task.TrackOnDispatcher(context.Dispatcher as AwaitableTaskDispatcher);

        public static Task TrackTaskOnDispatcher(this IActorContext context, Task task) => task.TrackOnDispatcher(context.Dispatcher as AwaitableTaskDispatcher);
    }
}
