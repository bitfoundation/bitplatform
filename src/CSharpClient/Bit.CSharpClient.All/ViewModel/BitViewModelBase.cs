using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Threading.Tasks;

namespace Bit.ViewModel
{
    public class BitViewModelBase : BindableBase, INavigatedAware, INavigatingAware, INavigationAware, IDestructible
    {
        public async void Destroy()
        {
            try
            {
                await DestroyAsync();
                await Task.Yield();
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

        public async void OnNavigatedFrom(INavigationParameters parameters)
        {
            try
            {
                await OnNavigatedFromAsync(parameters);
                await Task.Yield();
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatedFromAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public async void OnNavigatedTo(INavigationParameters parameters)
        {
            try
            {
                await Task.Yield();
                await OnNavigatedToAsync(parameters);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public async void OnNavigatingTo(INavigationParameters parameters)
        {
            try
            {
                await Task.Yield();
                await OnNavigatingToAsync(parameters);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        public virtual Task OnNavigatingToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }
    }
}
