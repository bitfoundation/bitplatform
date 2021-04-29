using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitDropDown
    {
        [Parameter] public List<DropDownItem> Items { get; set; } = new List<DropDownItem>();
        [Parameter] public string Placeholder { get; set; }

        protected override string RootElementClass => "bit-drp";
        protected override void RegisterComponentClasses()
        {
            //ClassBuilder.Register(() => IsChecked is true ?
            //    $"{RootElementClass}-checked-{VisualClassRegistrar()}" : string.Empty);
        }
    }
}
