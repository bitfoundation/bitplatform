﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public class BitDropdownCustom
{
    public string? Label { get; set; }

    public string? Key { get; set; }

    public object? Payload { get; set; }

    public bool Disabled { get; set; }

    public bool Visible { get; set; } = true;

    public bool IsSelected { get; set; }

    public BitDropdownItemType Type { get; set; } = BitDropdownItemType.Normal;

    public string? Text { get; set; }

    public string? Title { get; set; }

    public string? Value { get; set; }
}
