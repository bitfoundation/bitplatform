using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Pages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.ViewModel.Implementations
{
    public class BitPopupNavigation : IPopupNavigation
    {
        public IPopupNavigation OriginalImplementation { get; set; }

        public IReadOnlyList<PopupPage> PopupStack => OriginalImplementation.PopupStack;

        public async Task PopAllAsync(bool animate = true)
        {
            try
            {
                await OriginalImplementation.PopAllAsync(animate);
            }
            catch (IndexOutOfRangeException exp) when (exp.Message == "There is not page in PopupStack")
            {

            }
        }

        public async Task PopAsync(bool animate = true)
        {
            try
            {
                await OriginalImplementation.PopAsync(animate);
            }
            catch (IndexOutOfRangeException exp) when (exp.Message == "There is not page in PopupStack")
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
