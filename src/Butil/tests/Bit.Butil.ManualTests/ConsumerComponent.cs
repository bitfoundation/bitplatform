using Bit.Butil;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ButilManualTests;

/// <summary>
/// Stands in for a consumer's Blazor component. It exists to create exactly the kind of reference
/// that <c>@inject</c> creates, because that reference is what the trimming behaviour hinges on.
/// </summary>
/// <remarks>
/// A razor <c>@inject LocalStorage LocalStorage</c> compiles down to an <see cref="InjectAttribute"/>
/// decorated property, and Blazor fills it through the non-generic
/// <see cref="IServiceProvider.GetService(Type)"/>. So the only static reference the trimmer sees is
/// the property's <b>type</b> - never its constructor. That is why this class resolves through the
/// non-generic overload too: calling <c>GetRequiredService&lt;LocalStorage&gt;()</c> would annotate the
/// type argument with <c>PublicConstructors</c> and preserve the constructor by itself, which would
/// quietly defeat the very thing this project is meant to verify.
/// </remarks>
internal sealed class ConsumerComponent
{
    [Inject] public LocalStorage LocalStorage { get; set; } = default!;

    [Inject] public Clipboard Clipboard { get; set; } = default!;

    [Inject] public Cookie Cookie { get; set; } = default!;

    /// <summary>
    /// The Butil services this component injects. <c>typeof</c> references the type without preserving
    /// its constructors, matching what the property declarations above already do.
    /// </summary>
    public static Type[] InjectedTypes => [typeof(LocalStorage), typeof(Clipboard), typeof(Cookie)];

    public void Inject(IServiceProvider serviceProvider)
    {
        LocalStorage = (LocalStorage)serviceProvider.GetRequiredService(typeof(LocalStorage));
        Clipboard = (Clipboard)serviceProvider.GetRequiredService(typeof(Clipboard));
        Cookie = (Cookie)serviceProvider.GetRequiredService(typeof(Cookie));
    }

    /// <summary>
    /// Calls into each injected service so the reference is a real usage rather than an unused field
    /// the compiler or the trimmer could reason away.
    /// </summary>
    /// <remarks>
    /// The JS runtime is a stub, so return values are meaningless and a call may well throw - only the
    /// fact that the instances were constructed and are callable matters here.
    /// </remarks>
    public async Task Use()
    {
        await LocalStorage.SetItem("butil-manual-test", "1");
        await LocalStorage.GetItem("butil-manual-test");
        await Clipboard.IsSupported();
        await Cookie.GetValue("butil-manual-test");
    }
}
