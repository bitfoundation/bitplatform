using Prism.Navigation;

namespace Prism.Regions
{
    public static class IRegionManagerExtensions
    {
        public static void RequestNavigate(this IRegionManager regionManager, string regionName, string target, params (string, object)[] parameters)
        {
            regionManager.RequestNavigate(regionName, target, ConvertToINavigationParameters(parameters));
        }

        static INavigationParameters ConvertToINavigationParameters(params (string, object)[] parameters)
        {
            INavigationParameters navigationParameters = new NavigationParameters();

            foreach ((string, object) parameter in parameters)
            {
                navigationParameters.Add(parameter.Item1, parameter.Item2);
            };

            return navigationParameters;
        }
    }
}
