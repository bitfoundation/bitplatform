using Bit.ViewModel;
using Prism.Regions;
using Prism.Regions.Navigation;
using System.Threading.Tasks;

namespace Bit.CSharpClientSample.ViewModels
{
    public class RegionCViewModel : BitViewModelBase
    {
        public string Text { get; set; } = "C";

        public BitDelegateCommand GoToDRegionCommand { get; set; }

        public RegionCViewModel()
        {
            GoToDRegionCommand = new BitDelegateCommand(GoToDRegion);
        }

        async Task GoToDRegion()
        {
            RegionManager.RequestNavigate("ContentRegion2", "RegionD");
        }
    }
}
