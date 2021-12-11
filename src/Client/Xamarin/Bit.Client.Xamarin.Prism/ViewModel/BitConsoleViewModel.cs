using Bit.Core.Models;
using Bit.ViewModel.Implementations;
using Rg.Plugins.Popup.Contracts;
using System.Threading.Tasks;

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
#if Xamarin
            await Xamarin.Essentials.Clipboard.SetTextAsync(thing.ToString());
#elif NET6_0_ANDROID || NET6_0_IOS
            await Microsoft.Maui.Essentials.Clipboard.SetTextAsync(thing.ToString());
#endif
        }

        async Task Close()
        {
            await PopupNavigation.PopAsync();
        }
    }
}
