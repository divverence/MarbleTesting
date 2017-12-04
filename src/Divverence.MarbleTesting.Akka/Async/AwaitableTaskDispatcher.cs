using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Akka.Dispatch;

namespace Divverence.MarbleTesting.Akka.Async
{
    /// <summary>
    /// Task based dispatcher
    /// Copy of the Akka.Net standard TaskDispatcher, but this one remembers
    /// its tasks and lets unit tests await them to know the system is idle.
    /// </summary>
    public class AwaitableTaskDispatcher : MessageDispatcher, IWaitForIdleOrActive
    {
        public new static string Id = "divverence.marble-testing.akka.async.awaitable-task-dispatcher";

        private volatile TaskCompletionSource<bool> _activeTcs = new TaskCompletionSource<bool>();

        public Task Active()
        {
            return _activeTcs.Task;
        }

        public static Task NotIdle(params AwaitableTaskDispatcher[] subjects)
        {
            return Task.WhenAny(subjects.Select(subject => subject.Active()));
        }

        public static async Task Idle(params AwaitableTaskDispatcher[] subjects)
        {
            while (true)
            {
                var array = subjects.SelectMany(subject => subject._tasks).ToArray();
                await Task.WhenAll(array);
                var noMoreTasksAdded = array.Length == subjects.Sum(subject => subject._tasks.Count);
                if (noMoreTasksAdded)
                {
                    foreach (var subject in subjects)
                        subject._activeTcs = new TaskCompletionSource<bool>();
                    return;
                }
            }
        }

        public Task Idle()
        {
            return Idle(this);
        }

        private readonly ConcurrentBag<Task> _tasks = new ConcurrentBag<Task>();

        /// <summary>
        /// Takes a <see cref="MessageDispatcherConfigurator"/>
        /// </summary>
        public AwaitableTaskDispatcher(MessageDispatcherConfigurator configurator) : base(configurator)
        {
        }

        public void PoolTask(Task task)
        {
            _tasks.Add(task);
            _activeTcs.TrySetResult(true);
        }

        protected override void ExecuteTask(IRunnable run)
        {
            _tasks.Add(Task.Run(() => run.Run()));
            _activeTcs.TrySetResult(true);
        }

        protected override void Shutdown()
        {
        }
    }
}
