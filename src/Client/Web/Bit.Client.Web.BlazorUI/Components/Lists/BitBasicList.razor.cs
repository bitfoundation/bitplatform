using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitBasicList<TItem>
    {
        /// <summary>
        /// list of items that want to render
        /// </summary>
        [Parameter] public ICollection<TItem> Items { get; set; } = Array.Empty<TItem>();     
        /// <summary>
        /// virtualize rendering the list
        /// UI rendering to just the parts that are currently visible
        /// defualt is false
        /// </summary>
        [Parameter] public bool Virtualize { get; set; } = false;
        /// <summary>
        /// a placeholder to display content until the item data is available.
        /// </summary>
        [Parameter] public string? LoadingText { get; set; }
        /// <summary>
        /// determines how many additional items are rendered before and after the visible region
        /// defualt is 3
        /// </summary>
        [Parameter] public int OverscanCount { get; set; } = 3;
        /// <summary>
        /// The height of each item in pixels, defualt is 50
        /// </summary>
        [Parameter] public int ItemSize { get; set; } = 50;
        /// <summary>
        /// content of each item
        /// it should determin with context attribute
        /// </summary>
        [Parameter] public RenderFragment<TItem>? RowTemplate { get; set; }
        [Parameter] public string Role { get; set; } = "list";

        protected override string RootElementClass => "bit-bsc-lst";
        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Items):
                        Items = (System.Collections.Generic.ICollection<TItem>)parameter.Value;
                        break;
                    case nameof(Role):
                        Role = (string)parameter.Value;
                        break;
                    case nameof(Virtualize):
                        Virtualize = (bool)parameter.Value;
                        break;
                    case nameof(LoadingText):
                        LoadingText = (string)parameter.Value;
                        break;
                    case nameof(OverscanCount):
                        OverscanCount = (int)parameter.Value;
                        break;
                    case nameof(ItemSize):
                        ItemSize = (int)parameter.Value;
                        break;
                    case nameof(RowTemplate):
                        RowTemplate = (Microsoft.AspNetCore.Components.RenderFragment<TItem>)parameter.Value;
                        break;
                }
            }
            return base.SetParametersAsync(parameters);
        }
    }
}
