using Bit.ViewModel;
using Bit.ViewModel.Contracts;

namespace Bit.CSharpClientSample.ViewModels
{
    public class TestViewModel : BitViewModelBase
    {
        public TestViewModel(IPopupNavigationService popupNavigationService)
        {
            Close = new BitDelegateCommand(async () =>
            {
                await popupNavigationService.PopAsync();
            });
        }

        public virtual BitDelegateCommand Close { get; set; }
    }
}
