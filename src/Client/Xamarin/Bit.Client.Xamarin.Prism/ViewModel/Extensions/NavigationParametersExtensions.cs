using System;

namespace Prism.Navigation
{
    public static class NavigationParametersExtensions
    {
        public static bool TryGetNavigationMode(this INavigationParameters parameters, out NavigationMode navigationMode)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

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
