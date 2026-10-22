namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.RendererBoundary;

public partial class BitAiRendererBoundaryDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content to protect - typically a custom renderer of model output. It is rendered with no element of the boundary's own around it. What it throws while it renders, in the lifecycle methods of its components or in their event handlers replaces it with the notice; what a fire-and-forget task throws does not reach the boundary. For a throw to be caught it has to happen inside a component of the content: a RenderFragment that throws while the boundary itself builds it is the parent's failure.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiRendererBoundaryClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the renderer boundary.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "What the notice says once the content has failed. Left null, the RenderFailed text of the BitAiStrings in force (\"This content could not be rendered\") is shown. The message of the exception is the notice's tooltip either way.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiRendererBoundaryClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the renderer boundary.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiRendererBoundaryClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiRendererBoundary, which is rendered only once its content has failed: the block the notice stands in.",
                },
                new()
                {
                    Name = "Notice",
                    Type = "BitTagClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the notice (a BitTag) the BitAiRendererBoundary renders in place of its failed content.",
                    LinkType = LinkType.Link,
                    Href = "/components/tag#tag-class-styles",
                },
            ],
        },
    ];



    private readonly string osloForecast = """{"city":"Oslo","degrees":-3,"condition":"Snow"}""";

    // The same forecast in a shape the card was not written for: its temperature is an object, not "degrees".
    private readonly string unexpectedForecast = """{"city":"Lisbon","temperature":{"value":17,"unit":"C"},"condition":"Clear"}""";



    private const string aucklandForecast = """{"city":"Auckland","degrees":22,"condition":"Showers"}""";
    private string streamed = "";
    private bool isStreaming;

    private async Task StreamForecastAsync()
    {
        isStreaming = true;
        streamed = "";

        // A model writes the JSON a few characters at a time; every render on the way tries the card again.
        foreach (var chunk in aucklandForecast.Chunk(12))
        {
            streamed += new string(chunk);
            StateHasChanged();
            await Task.Delay(400);
        }

        isStreaming = false;
    }
}
