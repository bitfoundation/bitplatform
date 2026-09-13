using Microsoft.AspNetCore.Components;
using Bit.Butil.Demo.Client.Pages;

namespace Bit.Butil.Demo.Client.Docs;

/// <summary>
/// How widely the underlying browser API is implemented. This is about the web platform, not about
/// Butil: every wrapper on this site works everywhere Blazor does, but it can only expose what the
/// browser underneath it implements.
/// </summary>
public enum ApiSupport
{
    /// <summary>Not a browser API at all - a guide page.</summary>
    Guide,

    /// <summary>Implemented by every current engine.</summary>
    Broad,

    /// <summary>Implemented everywhere, but with members or behaviour that differ between engines.</summary>
    Partial,

    /// <summary>Chromium only (Chrome, Edge, Opera and friends).</summary>
    Chromium,

    /// <summary>Chromium on desktop only.</summary>
    ChromiumDesktop,

    /// <summary>Chromium on Android only.</summary>
    ChromiumMobile,
}
