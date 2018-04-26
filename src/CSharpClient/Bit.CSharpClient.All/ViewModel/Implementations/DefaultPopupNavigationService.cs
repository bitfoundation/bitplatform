using Bit.ViewModel.Contracts;
using Prism.Ioc;
using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Pages;
using System.Threading.Tasks;

namespace Bit.ViewModel.Implementations
{
    public class DefaultPopupNavigationService : IPopupNavigationService
    {
        private readonly IPopupNavigation _popupNavigation;
        private readonly IContainerProvider _containerProvider;

        public DefaultPopupNavigationService(IPopupNavigation popupNavigation, IContainerProvider containerProvider)
        {
            _popupNavigation = popupNavigation;
            _containerProvider = containerProvider;
        }

        public virtual async Task PopAllAsync()
        {
            await _popupNavigation.PopAllAsync().ConfigureAwait(false);
        }

        public virtual async Task PopAsync()
        {
            await _popupNavigation.PopAsync().ConfigureAwait(false);
        }

        public virtual async Task PushAsync(string name)
        {
            await _popupNavigation.PushAsync(_containerProvider.Resolve<PopupPage>(name)).ConfigureAwait(false);
        }
    }
}
