using Bit.ViewModel.Contracts;
using Prism.Navigation;
using System;
using System.Threading.Tasks;

namespace Bit.ViewModel.Implementations
{
    public class DefaultNavService : INavService
    {
        public static DefaultNavService Current { get; set; }

        public virtual INavigationService PrismNavigationService { get; set; }

        public virtual async Task NavigateAsync(string name, INavigationParameters parameters = null)
        {
            INavigationResult navigationResult = await PrismNavigationService.NavigateAsync(name, parameters, useModalNavigation: false, animated: false);
            if (!navigationResult.Success)
                throw navigationResult.Exception;
        }

        public virtual async Task NavigateAsync(string name, params (string, object)[] parameters)
        {
            await NavigateAsync(name, ConvertToINavigationParameters(parameters));
        }

        public virtual async Task NavigateAsync(Uri uri, INavigationParameters parameters = null)
        {
            INavigationResult navigationResult = await PrismNavigationService.NavigateAsync(uri, parameters, useModalNavigation: false, animated: false);
            if (!navigationResult.Success)
                throw navigationResult.Exception;
        }

        public virtual async Task NavigateAsync(Uri uri, params (string, object)[] parameters)
        {
            await NavigateAsync(uri, ConvertToINavigationParameters(parameters));
        }

        public virtual async Task GoBackAsync(INavigationParameters parameters = null)
        {
            if (Current?.PrismNavigationService == null)
                throw new InvalidOperationException($"Current nav service is not initialized.");

            // We use application level nav service (Current), because its GoBackAsync works across both pages & popups.
            // For example, if a popup calls GoBackAsync two times, the first one closes the popup itself, but the second one won't close the behind page.
            // Note that ../.. is not working in popup pages at the moment.

            INavigationResult navigationResult = await Current.PrismNavigationService.GoBackAsync(parameters, useModalNavigation: false, animated: false);

            if (!navigationResult.Success)
                throw navigationResult.Exception;
        }

        public virtual async Task GoBackAsync(params (string, object)[] parameters)
        {
            await GoBackAsync(ConvertToINavigationParameters(parameters));
        }

        public virtual async Task GoBackToRootAsync(INavigationParameters parameters = null)
        {
            INavigationResult navigationResult = await PrismNavigationService.GoBackToRootAsync(parameters);
            if (!navigationResult.Success)
                throw navigationResult.Exception;
        }

        public virtual async Task GoBackToRootAsync(params (string, object)[] parameters)
        {
            await GoBackToRootAsync(ConvertToINavigationParameters(parameters));
        }

        public virtual string GetNavigationUriPath()
        {
            return PrismNavigationService.GetNavigationUriPath();
        }

        INavigationParameters ConvertToINavigationParameters(params (string, object)[] parameters)
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
