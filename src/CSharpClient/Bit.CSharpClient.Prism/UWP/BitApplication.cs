#if UWP
using Bit.View;
using Bit.ViewModel;
using System.Linq;
using System.Reflection;
using Windows.Foundation;
using Windows.UI.ViewManagement;
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
        /// Configures VersionTracking | RgPluginsPopup | BitCSharpClientControls (DatePicker, Checkbox, RadioButton, Frame) | Set Min Width and Height
        /// </summary>
        protected virtual void UseDefaultConfiguration()
        {
            VersionTracking.Track();
            Rg.Plugins.Popup.Popup.Init();
            BitCSharpClientControls.Init();
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size
            {
                Height = 1,
                Width = 1
            });
        }

        /// <summary>
        /// BitCSharpClientControls | RgPluginsPopup
        /// </summary>
        protected virtual Assembly[] GetBitRendererAssemblies()
        {
            return new[] { typeof(BitCSharpClientControls).GetTypeInfo().Assembly }
                .Union(Rg.Plugins.Popup.Popup.GetExtraAssemblies())
                .ToArray();
        }
    }
}
#endif
