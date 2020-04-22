using System;

namespace Bit.Universal.Core.Extensions
{
    public enum Workload
    {
        /// <summary>
        /// ASP.NET Core
        /// </summary>
        Server,
        /// <summary>
        /// Android/iOS/Windows/WPF/WindowsForms/Console/Blazor
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
                return _workload ?? throw new InvalidOperationException("Bit detects workload in android/iOS/uwp/aspnet/blazor/ but you've to set Workload in wpf, winforms and console apps.");
            }
            set
            {
                _workload = value;
            }
        }
#endif
    }
}
