using Bit.Model;
using Bit.ViewModel.Implementations;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Rg.Plugins.Popup.Contracts;

namespace Bit.ViewModel
{
    public class BitConsoleViewModel : Bindable
    {
        public LocalTelemetryService LocalTelemetryService { get; set; }

        public BitDelegateCommand<TrackedThing> CopyCommand { get; set; }

        public IPopupNavigation PopupNavigation { get; set; }

        public BitDelegateCommand CloseCommand { get; set; }

        public BitConsoleViewModel()
        {
            CopyCommand = new BitDelegateCommand<TrackedThing>(Copy);
            CloseCommand = new BitDelegateCommand(Close);
        }

        async Task Copy(TrackedThing thing)
        {
            await Clipboard.SetTextAsync(thing.ToString());
        }

        async Task Close()
        {
            await PopupNavigation.PopAsync();
        }
    }
}
