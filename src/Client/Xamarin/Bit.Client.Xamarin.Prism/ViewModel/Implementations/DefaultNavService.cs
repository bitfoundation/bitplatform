using Bit.View;
using Bit.ViewModel.Contracts;
using Prism.Common;
using Prism.Navigation;
using Prism.Regions;
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
        public static TINavService INavServiceFactory<TINavService>(INavigationService prismNavService, IPopupNavigation popupNavigation, IRegionManager regionManager)
            where TINavService : DefaultNavService, new()
        {
            var navService = new TINavService
            {
                PrismNavigationService = prismNavService,
                PopupNavigation = popupNavigation,
                RegionManager = regionManager
            };

            return navService;
        }

        public virtual async Task NavigateAsync(string name, INavigationParameters? parameters = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.StartsWith("/", StringComparison.InvariantCultureIgnoreCase))
            {
                await ClearPopupStackAsync(parameters);

                if (RegionManager.Regions.Any())
                    await PrismNavigationService.NavigateAsync("/PageWhichWeStayThereUntilRegionsAreDisposed");
            }

            INavigationResult navigationResult = await PrismNavigationService.NavigateAsync(name, parameters, useModalNavigation: false, animated: false);

            if (!navigationResult.Success)
                throw navigationResult.Exception;
        }

        public virtual async Task NavigateAsync(string name, params (string key, object value)[] parameters)
        {
            await NavigateAsync(name, ConvertToINavigationParameters(parameters));
        }

        public virtual async Task NavigateAsync(Uri uri, INavigationParameters? parameters = null)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (uri.LocalPath.StartsWith("/", StringComparison.InvariantCultureIgnoreCase))
            {
                if (RegionManager.Regions.Any())
                    await PrismNavigationService.NavigateAsync("/PageWhichWeStayThereUntilRegionsAreDisposed");

                await ClearPopupStackAsync(parameters);
            }

            INavigationResult navigationResult = await PrismNavigationService.NavigateAsync(uri, parameters, useModalNavigation: false, animated: false);

            if (!navigationResult.Success)
                throw navigationResult.Exception;
        }

        public virtual async Task NavigateAsync(Uri uri, params (string key, object value)[] parameters)
        {
            await NavigateAsync(uri, ConvertToINavigationParameters(parameters));
        }

        public virtual async Task GoBackAsync(INavigationParameters? parameters = null)
        {
            bool ignoreMeInNavStack = PopupNavigation.PopupStack.LastOrDefault()?.GetType().GetCustomAttribute<IgnoreMeInNavigationStackAttribute>() != null;

            INavigationResult navigationResult = await PrismNavigationService.GoBackAsync(parameters, useModalNavigation: false, animated: false);

            if (!navigationResult.Success && navigationResult.Exception is ArgumentOutOfRangeException && AppNavService != null)
            {
                // We use application level nav service (Current), because its GoBackAsync works across both pages & popups.
                // For example, if a popup calls GoBackAsync two times, the first one closes the popup itself, but the second one won't close the behind page.
                // Note that ../.. is not working in popup pages at the moment.

                await AppNavService.GoBackAsync(parameters);
                return;
            }

            if (!navigationResult.Success)
                throw navigationResult.Exception;

            if (ignoreMeInNavStack == true)
            {
                await GoBackAsync(parameters);
            }
        }

        public virtual async Task GoBackAsync(params (string key, object value)[] parameters)
        {
            await GoBackAsync(ConvertToINavigationParameters(parameters));
        }

        public virtual async Task GoBackToRootAsync(INavigationParameters? parameters = null)
        {
            INavigationResult navigationResult = await PrismNavigationService.GoBackToRootAsync(parameters);
            if (!navigationResult.Success)
                throw navigationResult.Exception;
        }

        public virtual async Task GoBackToRootAsync(params (string key, object value)[] parameters)
        {
            await GoBackToRootAsync(ConvertToINavigationParameters(parameters));
        }

        public virtual string GetNavigationUriPath()
        {
            return PrismNavigationService.GetNavigationUriPath();
        }

        protected virtual INavigationParameters ConvertToINavigationParameters(params (string key, object value)[] parameters)
        {
            INavigationParameters navigationParameters = new NavigationParameters();

            foreach ((string key, object value) parameter in parameters)
            {
                navigationParameters.Add(parameter.key, parameter.value);
            };

            return navigationParameters;
        }

        public virtual async Task ClearPopupStackAsync(params (string key, object value)[] parameters)
        {
            await ClearPopupStackAsync(parameters: ConvertToINavigationParameters(parameters));
        }

        public virtual async Task ClearPopupStackAsync(INavigationParameters? parameters = null)
        {
            INavigationResult navigationResult = await PrismNavigationService.ClearPopupStackAsync(parameters: parameters, animated: false);
            if (!navigationResult.Success)
                throw navigationResult.Exception;

            await PopupNavigation.PopAllAsync(animate: false); // all popups which are not managed by prism's nav service.
        }

        public virtual async Task GoBackToAsync(string name, params (string key, object value)[] parameters)
        {
            await GoBackToAsync(name, ConvertToINavigationParameters(parameters));
        }

        public virtual async Task GoBackToAsync(string name, INavigationParameters? parameters = null)
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

        protected virtual string GenerateBackUri(int timesToGoBack)
        {
            return string.Concat(Enumerable
                .Repeat("../", timesToGoBack));
        }

        public virtual Task SelectTabAsync(string name, params (string key, object value)[] parameters)
        {
            return SelectTabAsync(name, ConvertToINavigationParameters(parameters));
        }

        public virtual async Task SelectTabAsync(string name, INavigationParameters parameters = null)
        {
            var prismNavService = PrismNavigationService;

            var currentPage = ((IPageAware)prismNavService).Page;

            var canNavigate = await PageUtilities.CanNavigateAsync(currentPage, parameters);
            if (!canNavigate)
                throw new Exception($"IConfirmNavigation for {currentPage} returned false");

            TabbedPage tabbedPage = null;

            if (currentPage is TabbedPage _)
            {
                tabbedPage = (TabbedPage)currentPage;
            }
            if (currentPage.Parent is TabbedPage parent)
            {
                tabbedPage = parent;
            }
            else if (currentPage.Parent is NavigationPage navPage)
            {
                if (navPage.Parent != null && navPage.Parent is TabbedPage parent2)
                {
                    tabbedPage = parent2;
                }
            }

            if (tabbedPage == null)
                throw new Exception("No parent TabbedPage could be found");

            var tabToSelectedType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(name));
            if (tabToSelectedType is null)
                throw new Exception($"No View Type has been registered for '{name}'");

            Page? target = null;
            foreach (var child in tabbedPage.Children)
            {
                if (child.GetType() == tabToSelectedType)
                {
                    target = child;
                    break;
                }

                if (child is NavigationPage childNavPage)
                {
                    if (childNavPage.CurrentPage.GetType() == tabToSelectedType ||
                        childNavPage.RootPage.GetType() == tabToSelectedType)
                    {
                        target = child;
                        break;
                    }
                }
            }

            if (target is null)
                throw new Exception($"Could not find a Child Tab for '{name}'");

            var tabParameters = UriParsingHelper.GetSegmentParameters(name, parameters);

            tabbedPage.CurrentPage = target;
            PageUtilities.OnNavigatedFrom(currentPage, tabParameters);
            PageUtilities.OnNavigatedTo(target, tabParameters);
        }

        public INavigationService PrismNavigationService { get; set; } = default!;

        public IPopupNavigation PopupNavigation { get; set; } = default!;

        public IRegionManager RegionManager { get; set; } = default!;

        public virtual INavService AppNavService => BitApplication.Current.NavigationService;

        public virtual INavService CurrentPageNavService
        {
            get
            {
                NavigationPage? appNavPage = BitApplication.Current?.MainPage as NavigationPage;

                if (appNavPage == null)
                    appNavPage = (BitApplication.Current?.MainPage as MasterDetailPage)?.Detail as NavigationPage;

                INavService? navService = (appNavPage?.CurrentPage?.BindingContext as BitViewModelBase)?.NavigationService;

                return navService!;
            }
        }
    }
}
