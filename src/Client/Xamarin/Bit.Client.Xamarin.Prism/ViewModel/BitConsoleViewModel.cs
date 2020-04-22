using Bit.Model;
using Bit.ViewModel.Implementations;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Contracts;

namespace Bit.ViewModel
{
    public class BitConsoleViewModel : Bindable
    {
        public LocalTelemetryService LocalTelemetryService { get; set; } = default!;

        public IPopupNavigation PopupNavigation { get; set; } = default!;

        public BitDelegateCommand<TrackedThing> CopyCommand { get; set; }

        public BitDelegateCommand CloseCommand { get; set; }

        public BitConsoleViewModel()
        {
            CopyCommand = new BitDelegateCommand<TrackedThing>(Copy);
            CloseCommand = new BitDelegateCommand(Close);
        }

        async Task Copy(TrackedThing thing)
        {
#if XamarinEssentials
            await Xamarin.Essentials.Clipboard.SetTextAsync(thing.ToString());
#endif
        }

        async Task Close()
        {
            await PopupNavigation.PopAsync();
        }
    }
}
