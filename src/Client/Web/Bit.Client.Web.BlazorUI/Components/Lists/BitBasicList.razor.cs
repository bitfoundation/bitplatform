using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI;

public partial class BitBasicList<TItem>
{
    /// <summary>
    /// list of items that want to render
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    [Parameter] public ICollection<TItem> Items { get; set; } = Array.Empty<TItem>();
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// virtualize rendering the list
    /// UI rendering to just the parts that are currently visible
    /// defualt is false
    /// </summary>
    [Parameter] public bool Virtualize { get; set; } = false;

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

    /// <summary>
    /// Role of the BasicList.
    /// </summary>
    [Parameter] public string Role { get; set; } = "list";

    protected override string RootElementClass => "bit-bsc-lst";
}
