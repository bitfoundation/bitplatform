using System;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace Bit.Client.Web.Wasm.Implementation
{
    public class WebAssemblyAppInfo : IAppInfo
    {
        public virtual string PackageName => "AppPackageName";

        public virtual string Name => "AppName";

        public virtual string VersionString => "0.0.1";

        public virtual Version Version => new Version(VersionString);

        public virtual string BuildString => "";

        public virtual AppTheme RequestedTheme => AppTheme.Unspecified;

        public virtual void ShowSettingsUI()
        {

        }
    }
}
