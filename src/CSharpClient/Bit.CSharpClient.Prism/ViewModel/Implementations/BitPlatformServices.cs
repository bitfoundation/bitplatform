using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Bit.ViewModel.Implementations
{
    public class BitPlatformServices : IPlatformServices
    {
        public IPlatformServices OriginalPlatformService { get; set; }

        public bool IsInvokeRequired => OriginalPlatformService.IsInvokeRequired;

        public string RuntimePlatform => OriginalPlatformService.RuntimePlatform;

        public void BeginInvokeOnMainThread(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            OriginalPlatformService.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            });
        }

        public Ticker CreateTicker()
        {
            return OriginalPlatformService.CreateTicker();
        }

        public Assembly[] GetAssemblies()
        {
            return OriginalPlatformService.GetAssemblies();
        }

        public string GetMD5Hash(string input)
        {
            return OriginalPlatformService.GetMD5Hash(input);
        }

        public double GetNamedSize(NamedSize size, Type targetElementType, bool useOldSizes)
        {
            return OriginalPlatformService.GetNamedSize(size, targetElementType, useOldSizes);
        }

        public SizeRequest GetNativeSize(VisualElement view, double widthConstraint, double heightConstraint)
        {
            return OriginalPlatformService.GetNativeSize(view, widthConstraint, heightConstraint);
        }

        public Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken)
        {
            return OriginalPlatformService.GetStreamAsync(uri, cancellationToken);
        }

        public IIsolatedStorageFile GetUserStoreForApplication()
        {
            return OriginalPlatformService.GetUserStoreForApplication();
        }

        public void OpenUriAction(Uri uri)
        {
            OriginalPlatformService.OpenUriAction(uri);
        }

        public void QuitApplication()
        {
            OriginalPlatformService.QuitApplication();
        }

        public void StartTimer(TimeSpan interval, Func<bool> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            OriginalPlatformService.StartTimer(interval, () =>
            {
                try
                {
                    return callback();
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                    return false;
                }
            });
        }
    }
}
