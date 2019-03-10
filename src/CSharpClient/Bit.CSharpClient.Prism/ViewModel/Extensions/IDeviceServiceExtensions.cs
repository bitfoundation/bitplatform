using Bit.ViewModel;
using System;
using System.Threading.Tasks;

namespace Prism.Services
{
    public static class IDeviceServiceExtensions
    {
        public static void BeginInvokeOnMainThreadAsync(this IDeviceService deviceService, Func<Task> func)
        {
            deviceService.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await func();
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            });
        }
    }
}
