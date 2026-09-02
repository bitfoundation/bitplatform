namespace Bit.Butil;

/// <summary>
/// Whether the browser should keep its own navigation controls visible while an element is
/// fullscreen. Only a request that is granted honours this - it is a hint, and engines are free to
/// ignore it.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/requestFullscreen#navigationui">Element.requestFullscreen() navigationUI</see>
/// </summary>
public enum FullScreenNavigationUI
{
    /// <summary>Let the browser decide. The default.</summary>
    Auto,

    /// <summary>Ask for the whole screen, with no browser chrome over it.</summary>
    Hide,

    /// <summary>Ask the browser to keep its navigation controls visible.</summary>
    Show
}
