using Bit.View;
using Bit.ViewModel.Contracts;
using Prism.Navigation;
using Rg.Plugins.Popup.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit.ViewModel.Implementations
{
    public class DefaultNavService : INavService
    {
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
            bool ignoreMeInNavStack = PopupNavigation.PopupStack.LastOrDefault()?.GetType().GetCustomAttribute<IgnoreMeInNavigationStackAttribute>() != null;

            INavigationResult navigationResult = await PrismNavigationService.GoBackAsync(parameters, useModalNavigation: false, animated: false);

            if (!navigationResult.Success && navigationResult.Exception is ArgumentOutOfRangeException && AppNavService != null)
            {
                // We use application level nav service (Current), because its GoBackAsync works across both pages & popups.
                // For example, if a popup calls GoBackAsync two times, the first one closes the popup itself, but the second one won't close the behind page.
                // Note that ../.. is not working in popup pages at the moment.

                await AppNavService.GoBackAsync(parameters);
            }

            if (!navigationResult.Success)
                throw navigationResult.Exception;

            if (ignoreMeInNavStack == true)
            {
                await GoBackAsync(parameters);
            }
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

        public virtual async Task ClearPopupStackAsync(params (string, object)[] parameters)
        {
            await ClearPopupStackAsync(parameters: ConvertToINavigationParameters(parameters));
        }

        public virtual async Task ClearPopupStackAsync(INavigationParameters parameters = null)
        {
            INavigationResult navigationResult = await PrismNavigationService.ClearPopupStackAsync(parameters: parameters, animated: false);
            if (!navigationResult.Success)
                throw navigationResult.Exception;

            await PopupNavigation.PopAllAsync(animate: false); // all popups which are not managed by prism's nav service.
        }

        public virtual async Task GoBackToAsync(string name, params (string, object)[] parameters)
        {
            await GoBackToAsync(name, ConvertToINavigationParameters(parameters));
        }

        public virtual async Task GoBackToAsync(string name, INavigationParameters parameters = null)
        {
            await ClearPopupStackAsync(parameters); // TODO

            string navigationStack = GetNavigationUriPath();
            List<string> pagesInStack = navigationStack.Split('/').ToList();
            int timesToGoBack = (pagesInStack.Count - 1) - pagesInStack.IndexOf(name);

            if (timesToGoBack == -1)
                throw new InvalidOperationException($"{name} could not be found in navigation stack");
            if (timesToGoBack == 0) // we are already in that page
                throw new InvalidOperationException("Already in the same page");

            string backUri = GenerateBackUri(timesToGoBack);

            await NavigateAsync(backUri, parameters);
        }

        string GenerateBackUri(int timesToGoBack)
        {
            return string.Concat(Enumerable
                .Repeat("../", timesToGoBack));
        }

        public virtual INavigationService PrismNavigationService { get; set; }

        public virtual IPopupNavigation PopupNavigation { get; set; }

        public virtual INavService AppNavService
        {
            get
            {
                return BitApplication.Current?.NavigationService;
            }
        }

        public virtual INavService CurrentPageNavService
        {
            get
            {
                NavigationPage appNavPage = BitApplication.Current?.MainPage as NavigationPage;

                if (appNavPage == null)
                    appNavPage = (BitApplication.Current?.MainPage as MasterDetailPage)?.Detail as NavigationPage;

                INavService navService = (appNavPage?.CurrentPage?.BindingContext as BitViewModelBase)?.NavigationService;

                return navService;
            }
        }
    }
}
