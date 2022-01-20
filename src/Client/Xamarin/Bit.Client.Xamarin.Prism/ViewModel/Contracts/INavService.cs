using Prism.Navigation;
using Prism.Regions;
using Rg.Plugins.Popup.Contracts;
using System;
using System.Threading.Tasks;

namespace Bit.ViewModel.Contracts
{
    public delegate INavService INavServiceFactory(INavigationService navigationService, IPopupNavigation popupNavigation, IRegionManager regionManager, AnimateNavigation animateNavigation);
    public delegate bool AnimateNavigation();

    /// <summary>
    /// It can be easily mocked.
    /// Its go back async works better in popup pages.
    /// It throws an exception when navigation methods' result becomes unsuccessful.
    /// Its clear popup stack method clears popups which are not being managed with prism.
    /// It has GoBackToAsync which accepts a name in navigation stack on goes back till required.
    /// </summary>
    public interface INavService
    {
        Task NavigateAsync(string name, INavigationParameters? parameters = null);
        Task NavigateAsync(string name, params (string key, object value)[] parameters);
        Task NavigateAsync(Uri uri, INavigationParameters? parameters = null);
        Task NavigateAsync(Uri uri, params (string key, object value)[] parameters);

        Task GoBackAsync(params (string key, object value)[] parameters);
        Task GoBackAsync(INavigationParameters? parameters = null);

        Task GoBackToRootAsync(params (string key, object value)[] parameters);
        Task GoBackToRootAsync(INavigationParameters? parameters = null);

        string GetNavigationUriPath();

        Task ClearPopupStackAsync(params (string key, object value)[] parameters);
        Task ClearPopupStackAsync(INavigationParameters? parameters = null);

        Task GoBackToAsync(string name, params (string key, object value)[] parameters);
        Task GoBackToAsync(string name, INavigationParameters? parameters = null);

        Task SelectTabAsync(string name, params (string key, object value)[] parameters);

        Task SelectTabAsync(string name, INavigationParameters? parameters = null);

        INavigationService PrismNavigationService { get; }

        INavService AppNavService { get; }

        /// <summary>
        /// In a sample nav stack( Page1/Page2/Page3) which you're executing a code in page2, sometimes you need page3's nav service.
        /// </summary>
        INavService CurrentPageNavService { get; }
    }
}
