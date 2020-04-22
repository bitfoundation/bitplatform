using System;

namespace Bit.Core.Contracts
{
    public delegate string GetNavigationUriPath();

    public delegate bool StartTimerAction();

    public delegate void StartTimer(TimeSpan interval, StartTimerAction action);

    public class DependencyDelegates
    {
        private static DependencyDelegates? _dependencyDelegates;

        public static DependencyDelegates Current => _dependencyDelegates ??= new DependencyDelegates();

        public StartTimer? StartTimer { get; set; } // Prism's DeviceService.StartTimer

        public GetNavigationUriPath? GetNavigationUriPath { get; set; } // BitApplication.Current.NavigationService.CurrentPageNavService.GetNavigationUriPath
    }
}
