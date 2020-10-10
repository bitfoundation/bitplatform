using Bit.ViewModel;
using Prism.Navigation;
using System.Linq;
using System.Threading.Tasks;

namespace Prism.Regions
{
    public static class IRegionManagerExtensions
    {
        public static async Task DestroyRegions(this IRegionManager regionManager, params string[] regionNames)
        {
            await Task.WhenAll(regionManager.Regions.Where(r => regionNames.Contains(r.Name)).SelectMany(r => r.Views.Select(v => v.BindingContext))
                .OfType<BitViewModelBase>()
                .Select(b => b.OnDestroyAsync())).ConfigureAwait(false);

            foreach (string regionForCleanup in regionNames)
            {
                regionManager.Regions.Remove(regionForCleanup);
            }
        }

        public static async Task RequestNavigateAsync(this IRegionManager regionManager, string regionName, string target, params (string, object)[] parameters)
        {
            await regionManager.RequestNavigateAsync(regionName, target, ConvertToINavigationParameters(parameters)).ConfigureAwait(false);
        }

        public static async Task RequestNavigateAsync(this IRegionManager regionManager, string regionName, string target, INavigationParameters parameters)
        {
            TaskCompletionSource<object?> tsc = new TaskCompletionSource<object?>();

            regionManager.RequestNavigate(regionName, target, result =>
            {
                if (result.Error != null)
                    tsc.SetException(result.Error);
                else
                    tsc.SetResult(null);
            }, parameters);

            await tsc.Task.ConfigureAwait(false);
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
