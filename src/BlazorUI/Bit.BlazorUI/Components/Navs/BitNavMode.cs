﻿namespace Bit.BlazorUI;

/// <summary>
/// Defines the mode in which navigation is handled by the nav component.
/// </summary>
public enum BitNavMode
{
    /// <summary>
    /// the value of selected key will change using NavigationManager and the current url inside the component.
    /// </summary>
    Automatic,

    /// <summary>
    /// selected key changes will be sent back to the parent component and the component won't change its value.
    /// </summary>
    Manual
}
