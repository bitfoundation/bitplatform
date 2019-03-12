using System;
using System.Threading.Tasks;

namespace Prism.Services
{
    public static class IDeviceServiceExtensions
    {
        public static Task BeginInvokeOnMainThreadAsync(this IDeviceService deviceService, Func<Task> func)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            deviceService.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await func();
                    tcs.SetResult(null);
                }
                catch (Exception exp)
                {
                    tcs.SetException(exp);
                }
            });

            return tcs.Task;
        }
    }
}
