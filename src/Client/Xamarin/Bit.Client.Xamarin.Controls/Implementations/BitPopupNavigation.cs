using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Events;
using Rg.Plugins.Popup.Exceptions;
using Rg.Plugins.Popup.Pages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.Client.Xamarin.Controls.Implementations
{
    public class BitPopupNavigation : IPopupNavigation
    {
        public IPopupNavigation OriginalImplementation { get; set; } = default!;

        public IReadOnlyList<PopupPage> PopupStack => OriginalImplementation.PopupStack;

        public event EventHandler<PopupNavigationEventArgs> Pushing
        {
            add => OriginalImplementation.Pushing += value;
            remove => OriginalImplementation.Pushing -= value;
        }

        public event EventHandler<PopupNavigationEventArgs> Pushed
        {
            add => OriginalImplementation.Pushed += value;
            remove => OriginalImplementation.Pushed -= value;
        }

        public event EventHandler<PopupNavigationEventArgs> Popping
        {
            add => OriginalImplementation.Popping += value;
            remove => OriginalImplementation.Popping -= value;
        }

        public event EventHandler<PopupNavigationEventArgs> Popped
        {
            add => OriginalImplementation.Popped += value;
            remove => OriginalImplementation.Popped -= value;
        }

        public async Task PopAllAsync(bool animate = true)
        {
            try
            {
                await OriginalImplementation.PopAllAsync(animate);
            }
            catch (RGPopupStackInvalidException exp) when (exp.Message == "No Page in PopupStack")
            {

            }
        }

        public async Task PopAsync(bool animate = true)
        {
            try
            {
                await OriginalImplementation.PopAsync(animate);
            }
            catch (RGPopupStackInvalidException exp) when (exp.Message == "No Page in PopupStack")
            {

            }
        }

        public async Task PushAsync(PopupPage page, bool animate = true)
        {
            await OriginalImplementation.PushAsync(page, animate);
        }

        public async Task RemovePageAsync(PopupPage page, bool animate = true)
        {
            await OriginalImplementation.RemovePageAsync(page, animate);
        }
    }
}
