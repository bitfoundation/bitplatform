#if Android

using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Bit.Core.Models.Events;
using Bit.View;
using Bit.ViewModel;
using Bit.ViewModel.Implementations;
using Prism;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace Bit.Android
{
    public class BitFormsAppCompatActivity : FormsAppCompatActivity
    {
        private bool _useDefaultConfiguration = false;

        /// <summary>
        /// Configures Xamarin Essentials | RgPluginsPopup | Fast Renderers | Bit.Client.Xamarin.Controls (DatePicker, Frame)
        /// </summary>
        protected virtual void UseDefaultConfiguration(Bundle savedInstanceState)
        {
            _useDefaultConfiguration = true;
            Rg.Plugins.Popup.Popup.Init(this);
#if Xamarin
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Xamarin.Essentials.VersionTracking.Track();
#else
            Microsoft.Maui.Essentials.Platform.Init(this, savedInstanceState);
            Microsoft.Maui.Essentials.VersionTracking.Track();
#endif
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

        public async override void OnBackPressed()
        {
            if (_useDefaultConfiguration == true)
            {
                IBackPressedResult result = await PrismPlatform.OnBackPressed(this);
                if (!result.Success)
                    BitExceptionHandler.Current.OnExceptionReceived(result.Exception);
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
#if Xamarin
                Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
#else
                Microsoft.Maui.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
#endif
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            if (_useDefaultConfiguration)
            {
#if Xamarin
                Xamarin.Essentials.Platform.OnNewIntent(intent);
#else
                Microsoft.Maui.Essentials.Platform.OnNewIntent(intent);
#endif
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (_useDefaultConfiguration)
            {
#if Xamarin
                Xamarin.Essentials.Platform.OnResume(activity: null);
#else
                Microsoft.Maui.Essentials.Platform.OnResume(activity: null);
#endif
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
