﻿using System.Reflection.Metadata;

namespace Bit.BlazorUI;

public partial class BitMessageBox
{
    /// <summary>
    /// The body of the message box.
    /// </summary>
    [Parameter] public string? Body { get; set; }

    /// <summary>
    /// The text of the Ok button.
    /// </summary>
    [Parameter] public string? OkText { get; set; }

    /// <summary>
    /// The event callback for closing the message box.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// The title of the message box.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    private async Task CloseModal()
    {
        await OnClose.InvokeAsync();
    }

    private async Task OnOkClick()
    {
        await OnClose.InvokeAsync();
    }
}