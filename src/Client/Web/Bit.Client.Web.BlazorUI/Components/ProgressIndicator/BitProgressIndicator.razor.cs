using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitProgressIndicator
    {
        [Parameter] public string Label { get; set; } = string.Empty;
        [Parameter] public string Description { get; set; } = string.Empty;
        protected override string RootElementClass => "bit-pi";
    }
}
