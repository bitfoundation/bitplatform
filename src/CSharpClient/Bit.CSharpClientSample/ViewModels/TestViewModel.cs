using Bit.ViewModel;
using Prism.Navigation;
using System.Threading.Tasks;

namespace Bit.CSharpClientSample.ViewModels
{
    public class TestViewModel : BitViewModelBase
    {
        public TestViewModel(INavigationService navigationService)
        {
            Close = new BitDelegateCommand(async () =>
            {
                await navigationService.GoBackAsync();
            });
        }

        public virtual BitDelegateCommand Close { get; set; }

        public override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            return base.OnNavigatedToAsync(parameters);
        }
    }
}
