using System;

namespace Prism.Navigation
{
    public static class NavigationParametersExtensions
    {
        public static bool TryGetNavigationMode(this INavigationParameters parameters, out NavigationMode navigationMode)
        {
            try
            {
                navigationMode = parameters.GetNavigationMode();
                return true;
            }
            catch
            {
                navigationMode = default;
                return false;
            }
        }
    }
}
