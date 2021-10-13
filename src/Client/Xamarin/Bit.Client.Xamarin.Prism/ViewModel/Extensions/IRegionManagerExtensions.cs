using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Navigation;

namespace Prism.Regions
{
    public static class IRegionManagerExtensions
    {
        public static async Task NavigateAsync(this IRegionManager regionManager, string regionName, string target, INavigationParameters parentParameters, params (string key, object value)[] parameters)
        {
            Exception? exception = null;

            regionManager.RequestNavigate(regionName, target, result =>
            {
                exception = result.Error;
            }, ConvertToINavigationParameters(parentParameters, parameters));

            if (exception != null)
                throw exception;
        }

        static INavigationParameters ConvertToINavigationParameters(INavigationParameters parentParameters, params (string key, object value)[] parameters)
        {
            INavigationParameters navigationParameters = new NavigationParameters().SetNavigationMode(NavigationMode.New);

            foreach ((string key, object value) parameter in parameters)
            {
                navigationParameters.Add(parameter.key, parameter.value);
            };

            foreach (KeyValuePair<string, object> parameter in parentParameters)
            {
                navigationParameters.Add(parameter.Key, parameter.Value);
            }

            return navigationParameters;
        }

        public static IRegionManager DestroyRegion(this IRegionManager regionManager, string regionName)
        {
            IRegion region = regionManager.Regions[regionName];

            region.RemoveAll();

            regionManager.Regions.Remove(regionName);

            return regionManager;
        }
    }
}
