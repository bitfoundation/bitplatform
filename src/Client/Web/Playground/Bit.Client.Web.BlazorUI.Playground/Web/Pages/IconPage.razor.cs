using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class IconPage
    {
        private List<string> FilteredIcons;

        protected override void OnInitialized()
        {
            FilteredIcons = Enum.GetValues(typeof(BitIconName))
                .Cast<BitIconName>()
                .Select(v => v.GetName())
                .ToList();
            base.OnInitialized();
        }

        private void SearchIcon()
        {

        }
    }
}
