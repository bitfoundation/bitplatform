using System;

namespace Bit.Core.Extensions
{
    public enum Workload
    {
        /// <summary>
        /// ASP.NET Core
        /// </summary>
        Server,
        /// <summary>
        /// Android/iOS/Windows(WPF/WindowsForms/UWP)/Blazor/Unity/
        /// </summary>
        Client
    }

    public static class AppUtilities
    {
#if Android || iOS || UWP
        public static Workload Workload => Workload.Client;
#else
        static Workload? _workload;

        public static Workload Workload
        {
            get
            {
#if DotNet
                if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Wasm)
                    return Workload.Client;
#endif
                return _workload ?? throw new InvalidOperationException("Bit detects workload in android/iOS/uwp/blazor-wasm/ but you've to set Workload in other application models.");
            }
            set
            {
                _workload = value;
            }
        }
#endif
    }
}
