using Bit.ViewModel;
using System.Threading.Tasks;

namespace Bit.CSharpClientSample.ViewModels
{
    public class RegionBViewModel : BitViewModelBase
    {
        public BitDelegateCommand GoBackCommand { get; set; }

        public RegionBViewModel()
        {
            GoBackCommand = new BitDelegateCommand(GoBack);
        }

        async Task GoBack()
        {
            RegionManager.Regions["ContentRegion1"].NavigationService.Journal.GoBack();
        }

        public string Text { get; set; } = "B";
    }
}
