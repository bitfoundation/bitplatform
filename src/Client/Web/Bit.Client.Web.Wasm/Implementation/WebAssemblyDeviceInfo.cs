using System;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace Bit.Client.Web.Wasm.Implementation
{
    public class WebAssemblyDeviceInfo : IDeviceInfo
    {
        public virtual string Model => "DeviceModel";

        public virtual string Manufacturer => "DeviceManufacturer";

        public virtual string Name => "DeviceName";

        public virtual string VersionString => "0.0.1";

        public virtual Version Version => new Version(VersionString);

        public virtual DevicePlatform Platform => DevicePlatform.Create("Web");

        public virtual DeviceIdiom Idiom => DeviceIdiom.Unknown;

        public virtual DeviceType DeviceType => DeviceType.Physical;
    }
}
