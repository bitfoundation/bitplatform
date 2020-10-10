using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Plugin.Popups;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;

namespace Prism.Navigation
{
    public static partial class PopupExtensions
    {

        static INavigation s_navigation
        {
            get { return Application.Current.MainPage.Navigation; }
        }

        static IPopupNavigation s_popupNavigation => PopupNavigation.Instance;

        static IReadOnlyList<PopupPage> s_popupStack => s_popupNavigation.PopupStack;

        public static Task<INavigationResult> ClearPopupStackAsync(this INavigationService navigationService, string key, object param, bool animated = true) =>
            navigationService.ClearPopupStackAsync(GetNavigationParameters(key, param, NavigationMode.Back), animated);

        public static async Task<INavigationResult> ClearPopupStackAsync(this INavigationService navigationService, INavigationParameters parameters = null, bool animated = true)
        {
            while (s_popupStack.Any())
            {
                var result = await navigationService.GoBackAsync(parameters, null, animated: animated);
                if (result.Exception != null)
                    return result;
            }

            return new NavigationResult { Success = true };
        }

        private static INavigationParameters GetNavigationParameters(string key, object param, NavigationMode mode) =>
            new NavigationParameters()
            {
                { key, param }
            }.AddNavigationMode(mode);
    }
}
