using Bit.ViewModel;
using Prism.Navigation;
using Prism.Regions;
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
            await RegionNavigationJornal.NavigationTarget.RequestNavigateAsync("RegionB");
        }

        public async override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.GetNavigationMode() == NavigationMode.New)
            {

            }

            await base.OnNavigatedToAsync(parameters);
        }

        public override Task OnDestroyAsync()
        {
            return base.OnDestroyAsync();
        }
    }
}
