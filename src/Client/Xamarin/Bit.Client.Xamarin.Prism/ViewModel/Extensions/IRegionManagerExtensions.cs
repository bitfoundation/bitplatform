using Prism.Common;
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

        public static IRegionManager DestroyRegion(this IRegionManager regionManager, string regionName)
        {
            IRegion region = regionManager.Regions[regionName];

            foreach (var view in region.Views)
            {
                PageUtilities.InvokeViewAndViewModelAction<IDestructible>(view, destructible => destructible.Destroy());
            }

            regionManager.Regions.Remove(regionName);

            return regionManager;
        }
    }
}
