﻿using Bit.Core.Models;
using Bit.ViewModel.Implementations;
using Rg.Plugins.Popup.Contracts;
using System.Threading.Tasks;
using Xamarin.Essentials;

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
            await Clipboard.SetTextAsync(thing.ToString());
        }

        async Task Close()
        {
            await PopupNavigation.PopAsync();
        }
    }
}
