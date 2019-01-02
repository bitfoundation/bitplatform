using Prism.Navigation;
using System;
using System.Threading.Tasks;

namespace Bit.ViewModel.Contracts
{
    public interface INavService
    {
        Task NavigateAsync(string name, INavigationParameters parameters = null);
        Task NavigateAsync(string name, params (string, object)[] parameters);
        Task NavigateAsync(Uri uri, INavigationParameters parameters = null);
        Task NavigateAsync(Uri uri, params (string, object)[] parameters);

        Task GoBackAsync(params (string, object)[] parameters);
        Task GoBackAsync(INavigationParameters parameters = null);

        Task GoBackToRootAsync(params (string, object)[] parameters);
        Task GoBackToRootAsync(INavigationParameters parameters = null);

        string GetNavigationUriPath();
    }
}
