using Bit.ViewModel;
using System.Threading.Tasks;

namespace Bit.CSharpClientSample.ViewModels
{
    public class TestViewModel : BitViewModelBase
    {
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
