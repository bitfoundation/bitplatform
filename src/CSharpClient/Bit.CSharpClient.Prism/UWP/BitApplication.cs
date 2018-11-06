#if UWP
using Bit.ViewModel;
using Windows.UI.Xaml;
using Xamarin.Essentials;

namespace Bit.UWP
{
    public class BitApplication : Windows.UI.Xaml.Application
    {
        public BitApplication()
        {
            UnhandledException += BitApplication_UnhandledException;
        }

        private void BitApplication_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            BitExceptionHandler.Current.OnExceptionReceived(e.Exception);
        }

        /// <summary>
        /// Configures VersionTracking | RgPluginsPopup
        /// </summary>
        protected virtual void UseDefaultConfiguration()
        {
            VersionTracking.Track();
            Rg.Plugins.Popup.Popup.Init();
        }
    }
}
#endif