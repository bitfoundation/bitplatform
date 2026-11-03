using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class LandingHead
{
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    [Parameter, EditorRequired] public string Description { get; set; } = default!;

    [Parameter, EditorRequired] public string Path { get; set; } = default!;

    [Parameter] public LandingSite? Site { get; set; }

    [Parameter] public string? Image { get; set; }

    [Parameter] public string? Icon { get; set; }

    [Parameter] public JsonNode? StructuredData { get; set; }

    [Parameter] public bool NoIndex { get; set; }

    private string? structuredData;

    protected override void OnParametersSet()
    {
        structuredData = StructuredData?.ToJsonString().Replace("</", "<\\/", StringComparison.Ordinal);

        base.OnParametersSet();
    }

    private string Absolute(CultureInfo culture) => AbsoluteUrl(LandingCultures.Localize(Path, culture));
}
