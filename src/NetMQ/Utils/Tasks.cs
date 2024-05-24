using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetMQ.Utils
{
    internal class Tasks
    {
        internal static async Task PollUntil(Func<bool> condition, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);

            await PollUntil(condition, cts.Token);
        }

        internal static async Task PollUntil(Func<bool> condition, CancellationToken ct = default)
        {
            try
            {
                while (!condition())
                {
                    await Task.Delay(25, ct).ConfigureAwait(true);
                }
            }
            catch (TaskCanceledException)
            {
                // Task was cancelled. Ignore exception and return.
            }
        }

        internal static bool WaitAll(Task[] tasks, TimeSpan timeout)
        {
            return PollUntil(() => tasks.All(t => t.IsCompleted), timeout).Status == TaskStatus.RanToCompletion;
        }

        internal static bool WaitAll(Task[] tasks)
        {
            return WaitAll(tasks, TimeSpan.MaxValue);
        }

        internal static bool Wait(Task task, TimeSpan timeout)
        {
            return PollUntil(() => task.IsCompleted, timeout).Status == TaskStatus.RanToCompletion;
        }

        internal static bool Wait(Task task)
        {
            return Wait(task, TimeSpan.MaxValue);
        }
    }
}
