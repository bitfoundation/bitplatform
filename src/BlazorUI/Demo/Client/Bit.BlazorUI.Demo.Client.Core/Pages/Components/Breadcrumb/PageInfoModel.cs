﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Breadcrumb;

public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}
