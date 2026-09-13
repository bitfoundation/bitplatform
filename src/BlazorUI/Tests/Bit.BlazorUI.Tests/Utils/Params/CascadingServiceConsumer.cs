using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Utils.Params;

public interface ICascadingDemoService
{
    string Name { get; }
}

public class CascadingDemoService : ICascadingDemoService
{
    public string Name => "demo-service";
}

public sealed class CascadingDemoServiceDecorator : CascadingDemoService
{
}

/// <summary>
/// A service that is cascaded once and then mutated in place, which is what BitCascadingValue.NotifyChanged
/// is for: there is no assignment for the provider to notice.
/// </summary>
public sealed class MutableCascadingDemoService : ICascadingDemoService
{
    public string Name { get; set; } = "initial";
}

public sealed class CascadingServiceConsumer : ComponentBase
{
    [CascadingParameter] public ICascadingDemoService? Service { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, Service?.Name ?? "none");
    }
}
