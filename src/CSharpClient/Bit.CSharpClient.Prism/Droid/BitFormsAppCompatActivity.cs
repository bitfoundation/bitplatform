#if Android

using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Bit.View;
using Bit.ViewModel.Implementations;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace Bit.Android
{
    public class BitFormsAppCompatActivity : FormsAppCompatActivity
    {
        private bool _useDefaultConfiguration = false;

        /// <summary>
        /// Configures VersionTracking | RgPluginsPopup | Fast Renderers | Xamarin Essentials' Permissions  | BitCSharpClientControls (DatePicker, Checkbox, RadioButton, Frame)
        /// </summary>
        protected virtual void UseDefaultConfiguration(Bundle savedInstanceState)
        {
            ((AppDomainSetup)AppDomain.CurrentDomain.GetType().GetProperty("SetupInformationNoCopy", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(AppDomain.CurrentDomain))
                .ApplicationBase = "/"; // workaround for app insight which tries to read app config file.

            _useDefaultConfiguration = true;
            Rg.Plugins.Popup.Popup.Init(this, savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            VersionTracking.Track();
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

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (_useDefaultConfiguration == true)
            {
                Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}

#endif