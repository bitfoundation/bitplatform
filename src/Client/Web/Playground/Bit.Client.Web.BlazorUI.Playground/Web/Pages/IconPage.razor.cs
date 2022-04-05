using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class IconPage
    {
        private List<string> allIcons;
        private List<string> filteredIcons;
        private string searchText = string.Empty;

        protected override void OnInitialized()
        {
            allIcons = Enum.GetValues(typeof(BitIconName))
                .Cast<BitIconName>()
                .Select(v => v.GetName())
                .ToList();
            HandleClear();
            base.OnInitialized();
        }

        private void HandleClear()
        {
            filteredIcons = allIcons;
        }

        private void HandleChange(string text)
        {
            HandleClear();
            searchText = text;
            if (string.IsNullOrEmpty(text)) return;

            filteredIcons = allIcons.FindAll(icon => string.IsNullOrEmpty(icon) is false && icon.Contains(text, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
