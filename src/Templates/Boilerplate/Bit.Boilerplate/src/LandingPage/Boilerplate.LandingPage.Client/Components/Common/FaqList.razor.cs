using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class FaqList
{
    [Parameter, EditorRequired] public IReadOnlyList<LandingFaq> Items { get; set; } = [];

    [Parameter] public string? Class { get; set; }

    private string structuredData = "{}";

    protected override void OnParametersSet()
    {
        structuredData = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "FAQPage",
            ["mainEntity"] = new JsonArray([.. Items.Select(item => (JsonNode)new JsonObject
            {
                ["@type"] = "Question",
                ["name"] = item.Question,
                ["acceptedAnswer"] = new JsonObject { ["@type"] = "Answer", ["text"] = item.Answer }
            })])
        }.ToJsonString().Replace("</", "<\\/", StringComparison.Ordinal);

        base.OnParametersSet();
    }
}

public sealed record LandingFaq(string Question, string Answer);
