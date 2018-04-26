using Prism.Mvvm;
using Prism.Navigation;
using System.Threading.Tasks;

namespace Bit.ViewModel
{
    public class BitViewModelBase : BindableBase, INavigatedAware, INavigatingAware, INavigationAware, IDestructible, IConfirmNavigation, IConfirmNavigationAsync
    {
        public virtual bool CanNavigate(NavigationParameters parameters)
        {
            return true;
        }

        public virtual Task<bool> CanNavigateAsync(NavigationParameters parameters)
        {
            return Task.FromResult(true);
        }

        public virtual void Destroy()
        {

        }

        public virtual void OnNavigatedFrom(NavigationParameters parameters)
        {

        }

        public virtual void OnNavigatedTo(NavigationParameters parameters)
        {

        }

        public virtual void OnNavigatingTo(NavigationParameters parameters)
        {

        }
    }
}
