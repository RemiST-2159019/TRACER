using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tracer
{
    public static class CoroutineHelpers
    {
        public static IEnumerator WaitForTask(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }

        public static IEnumerator WaitForTask<T>(Task<T> task, System.Action<T> resultCallback)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
            resultCallback(task.Result);
        }
    }
}
