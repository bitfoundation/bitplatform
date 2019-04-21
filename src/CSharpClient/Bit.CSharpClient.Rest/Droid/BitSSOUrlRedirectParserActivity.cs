#if Android
using Android.App;
using Android.OS;
using Bit.ViewModel.Implementations;
using System;

namespace Bit.Android
{
    public class BitSSOUrlRedirectParserActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Uri uri = new Uri(Intent.Data.ToString());

            DefaultSecurityService.Current?.OnSsoLoginLogoutRedirectCompleted(uri);

            Finish();
        }
    }
}
#endif
