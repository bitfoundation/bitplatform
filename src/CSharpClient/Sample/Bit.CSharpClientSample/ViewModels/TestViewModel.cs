using Bit.ViewModel;
using System.Threading.Tasks;

namespace Bit.CSharpClientSample.ViewModels
{
    public class TestViewModel : BitViewModelBase
    {
        public virtual BitDelegateCommand CloseCommand { get; set; }

        public virtual BitDelegateCommand IncreaseStepsCountCommand { get; set; }

        public int StepsCount { get; set; }

        public TestViewModel()
        {
            CloseCommand = new BitDelegateCommand(Close);
            IncreaseStepsCountCommand = new BitDelegateCommand(IncreaseSteps);
        }

        async Task IncreaseSteps()
        {
            StepsCount++;
        }

        async Task Close()
        {
            await NavigationService.GoBackAsync();
        }
    }
}
