using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Utils.Params;

public sealed class FakeMergeParams : IBitComponentParams
{
    public string Name => "M";

    public int? Number { get; set; }

    public string? Text { get; set; }

    public Dictionary<string, object>? Attributes { get; set; }

    public IEnumerable<int>? Numbers { get; set; }

    public FakeMergeNested? Nested { get; set; }
}

public sealed class FakeMergeNested
{
    public List<string> Tags { get; } = [];
}

/// <summary>
/// Reads a <see cref="FakeMergeParams"/> and counts its own renders.
/// </summary>
public sealed class MergeConsumer : ComponentBase
{
    [CascadingParameter(Name = "M")] public FakeMergeParams? M { get; set; }

    [CascadingParameter(Name = "B")] public FakeParamsB? B { get; set; }

    public int RenderCount { get; private set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        RenderCount++;

        var attributes = M?.Attributes is null ? string.Empty : string.Join(",", M.Attributes.Keys);

        builder.AddContent(0, $"{M?.Number}|{M?.Text}|{attributes}|{B?.Text}");
    }
}

/// <summary>
/// A component with no parameters of its own, so a re-render of its parent never reaches what it renders: only a
/// cascading value that notifies its consumers does.
/// </summary>
public sealed class StaticMergeHost : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<MergeConsumer>(0);
        builder.CloseComponent();
    }
}
