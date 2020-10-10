using System;
using System.Linq;
using Prism.Behaviors;
using Prism.Common;
using Prism.Navigation;
using Prism.Plugin.Popups.Extensions;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms;

namespace Prism.Plugin.Popups
{
#if __IOS__
    [Foundation.Preserve(AllMembers = true)]
#elif __ANDROID__
    [Android.Runtime.Preserve(AllMembers = true)]
#endif
    public sealed class BackgroundPopupDismissalBehavior : BehaviorBase<PopupPage>
    {
        private IPopupNavigation _popupNavigation { get; }
        private IApplicationProvider _applicationProvider { get; }

        public BackgroundPopupDismissalBehavior(IPopupNavigation popupNavigation, IApplicationProvider applicationProvider)
        {
            _popupNavigation = popupNavigation;
            _applicationProvider = applicationProvider;
        }

        protected override void OnAttachedTo(PopupPage bindable)
        {
            base.OnAttachedTo(bindable);
            bindable.BackgroundClicked += OnBackgroundClicked;
        }

        protected override void OnDetachingFrom(PopupPage bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.BackgroundClicked -= OnBackgroundClicked;
        }

        private void OnBackgroundClicked(object sender, EventArgs e)
        {
            // If the Popup Page is not going to dismiss we don't need to do anything
            if (!AssociatedObject.CloseWhenBackgroundIsClicked) return;

            var parameters = PopupUtilities.CreateBackNavigationParameters();

            InvokePageInterfaces(AssociatedObject, parameters, false);
            InvokePageInterfaces(TopPage(), parameters, true);
        }

        private void InvokePageInterfaces(Page page, INavigationParameters parameters, bool navigatedTo)
        {
            PageUtilities.InvokeViewAndViewModelAction<INavigatedAware>(page, (view) =>
            {
                if (navigatedTo)
                    view.OnNavigatedTo(parameters);
                else
                    view.OnNavigatedFrom(parameters);
            });
            PageUtilities.InvokeViewAndViewModelAction<IActiveAware>(page, (view) => view.IsActive = navigatedTo);

            if (!navigatedTo)
                PageUtilities.InvokeViewAndViewModelAction<IDestructible>(AssociatedObject, (view) => view.Destroy());
        }

        private Page TopPage()
        {
            Page page = null;
            if (_popupNavigation.PopupStack.Any(p => p != AssociatedObject))
                page = _popupNavigation.PopupStack.LastOrDefault(p => p != AssociatedObject);
            else if (_applicationProvider.MainPage.Navigation.ModalStack.Count > 0)
                page = _applicationProvider.MainPage.Navigation.ModalStack.LastOrDefault();
            else
                page = _applicationProvider.MainPage.Navigation.NavigationStack.LastOrDefault();

            if (page == null)
                page = _applicationProvider.MainPage;

            return page.GetDisplayedPage();
        }
    }
}
