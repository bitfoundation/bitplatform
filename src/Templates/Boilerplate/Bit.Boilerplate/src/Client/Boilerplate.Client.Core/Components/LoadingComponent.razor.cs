﻿namespace Boilerplate.Client.Core.Components;

public partial class LoadingComponent
{
    [Parameter] public string Color { get; set; } = "#123456";

    [Parameter] public bool FullScreen { get; set; }
}
