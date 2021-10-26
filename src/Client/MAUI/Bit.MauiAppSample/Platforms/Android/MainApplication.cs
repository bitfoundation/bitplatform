using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using System;
using Microsoft.Maui.Hosting;

namespace Bit.MauiAppSample
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
