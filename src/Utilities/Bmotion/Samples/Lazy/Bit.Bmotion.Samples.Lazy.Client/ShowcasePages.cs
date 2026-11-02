namespace Bit.Bmotion.Samples.Lazy.Client;

/// <summary>
/// The pages that animate, which Bit.Bmotion.wasm is downloaded for: by the router's <c>OnNavigateAsync</c> as one
/// opens (Routes.razor), and by Program.cs for the one a visitor arrives on. It names none of Bmotion's types, so both
/// can use it before the assembly is there.
/// </summary>
internal static class ShowcasePages
{
    /// <summary>
    /// Set only where Bit.Bmotion.wasm is marked (the project file): loading an assembly that is not marked throws. The
    /// server, which prerenders the router too, does not set it, as it loads Bmotion with the rest of the app.
    /// </summary>
    public static readonly bool LazyLoadsBmotion = AppContext.TryGetSwitch("Bit.Bmotion.Samples.Lazy.LazyLoadsBmotion", out var on) && on;

    /// <summary>
    /// /showcase and the routes beneath it (routes match regardless of case), but not /showcases. The path is relative
    /// to the base address, and keeps any query and fragment.
    /// </summary>
    public static bool Contains(string path)
    {
        path = path.Split('?', '#')[0];
        return path.Equals("showcase", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("showcase/", StringComparison.OrdinalIgnoreCase);
    }
}
