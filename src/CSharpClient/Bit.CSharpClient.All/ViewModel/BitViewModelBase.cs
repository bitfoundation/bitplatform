using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Bit.ViewModel
{
    public class BitViewModelBase : BindableBase, INavigatedAware, INavigatingAware, INavigationAware, IDestructible
    {
        public async void Destroy()
        {
            try
            {
                await DestroyAsync().ConfigureAwait(false);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task DestroyAsync()
        {
            return Task.CompletedTask;
        }

        public async void OnNavigatedFrom(NavigationParameters parameters)
        {
            try
            {
                await OnNavigatedFromAsync(parameters).ConfigureAwait(false);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatedFromAsync(NavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public async void OnNavigatedTo(NavigationParameters parameters)
        {
            try
            {
                await OnNavigatedToAsync(parameters).ConfigureAwait(false);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatedToAsync(NavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public async void OnNavigatingTo(NavigationParameters parameters)
        {
            try
            {
                await OnNavigatingToAsync(parameters).ConfigureAwait(false);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatingToAsync(NavigationParameters parameters)
        {
            return Task.CompletedTask;
        }
    }
}
