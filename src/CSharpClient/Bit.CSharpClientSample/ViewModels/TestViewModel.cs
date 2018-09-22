using Bit.ViewModel;
using Prism.Navigation;
using System.Threading.Tasks;

namespace Bit.CSharpClientSample.ViewModels
{
    public class TestViewModel : BitViewModelBase
    {
        public INavigationService NavigationService { get; set; }

        public virtual BitDelegateCommand CloseCommand { get; set; }

        public TestViewModel()
        {
            CloseCommand = new BitDelegateCommand(Close);
        }

        async Task Close()
        {
            await NavigationService.GoBackAsync();
        }
    }
}
