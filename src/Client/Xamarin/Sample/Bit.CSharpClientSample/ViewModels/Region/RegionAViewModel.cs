using Bit.ViewModel;
using Prism.Navigation;
using Prism.Regions;
using Prism.Regions.Navigation;
using System;
using System.Threading.Tasks;

namespace Bit.CSharpClientSample.ViewModels
{
    public class RegionAViewModel : BitViewModelBase
    {
        public string Text { get; set; } = "A";

        public BitDelegateCommand GoToBRegionCommand { get; set; }

        public RegionAViewModel()
        {
            GoToBRegionCommand = new BitDelegateCommand(GoToBRegion);
        }

        async Task GoToBRegion()
        {
            await RegionManager.RequestNavigateAsync("ContentRegion1", "RegionB");
        }

        public async override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            await base.OnNavigatedToAsync(parameters);
        }

        public override Task OnDestroyAsync()
        {
            return base.OnDestroyAsync();
        }
    }
}
