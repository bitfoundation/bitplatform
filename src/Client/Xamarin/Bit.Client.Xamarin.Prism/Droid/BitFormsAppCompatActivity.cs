#if Android

using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Bit.View;
using Bit.ViewModel;
using Bit.ViewModel.Implementations;
using Prism.Events;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Prism.Ioc;
using Bit.Core.Models.Events;

namespace Bit.Android
{
#if Android9
    [Obsolete("Bit is going to drop support for Android 9 Sdk. It's recommended to use Android 10 Sdk.")]
#endif
    public class BitFormsAppCompatActivity : FormsAppCompatActivity
    {
        private bool _useDefaultConfiguration = false;

        /// <summary>
        /// Configures VersionTracking | RgPluginsPopup | Fast Renderers | Xamarin Essentials' Permissions  | Bit.Client.Xamarin.Controls (DatePicker, Frame)
        /// </summary>
        protected virtual void UseDefaultConfiguration(Bundle savedInstanceState)
        {
            _useDefaultConfiguration = true;
            Rg.Plugins.Popup.Popup.Init(this, savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Xamarin.Essentials.VersionTracking.Track();
            BitCSharpClientControls.Init();
            SetBitPlatformServices();
        }

        protected virtual async void SetBitPlatformServices()
        {
            while (true)
            {
                await Task.Delay(25);
                if (Forms.IsInitialized)
                {
                    Device.PlatformServices = new BitPlatformServices
                    {
                        OriginalPlatformService = Device.PlatformServices
                    };
                    break;
                }
            }
        }

        public override void OnBackPressed()
        {
            if (_useDefaultConfiguration == true)
            {
                Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed);
            }
            else
            {
                base.OnBackPressed();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[]? permissions, [GeneratedEnum] Permission[]? grantResults)
        {
            if (_useDefaultConfiguration == true)
            {
                Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (_useDefaultConfiguration)
            {
                Xamarin.Essentials.Platform.OnResume();
            }
        }

        public override void OnLowMemory()
        {
            try
            {
                if (LocalTelemetryService.Current.IsConfigured()) // LocalTelemetryService is not DI friendly.
                    LocalTelemetryService.Current.ClearThings();

                BitApplication.Current.Container?.Resolve<IEventAggregator>().GetEvent<LowMemoryEvent>().Publish(new LowMemoryEvent { });
            }
            catch (Exception exp) when (exp is ArgumentNullException && exp.Message == "Value cannot be null.\nParameter name: context")
            {
                // Do nothing. Container is not be ready at this time!
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
            finally
            {
                base.OnLowMemory();
            }
        }
    }
}

#endif