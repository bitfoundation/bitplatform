using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitDetailsList
    {
        protected override string RootElementClass => "bit-dt-lst";

        [Parameter] public bool IsCompactMode { get; set; }
        [Parameter] public bool IsModalSelection { get; set; }

        [Parameter] public string? AriaDescription { get; set; }
        [Parameter] public string SelectionDetails { get; set; } = "";

        [Parameter] public bool AriaHidden { get; set; }


        [Parameter] public IReadOnlyCollection<BitDocument> Items { get; set; } = new List<BitDocument>();
        [Parameter] public IReadOnlyCollection<BitColumn> Columns { get; set; } = new List<BitColumn>();
        [Parameter] public string ControlClassName { get; set; } = "";

    }
}
