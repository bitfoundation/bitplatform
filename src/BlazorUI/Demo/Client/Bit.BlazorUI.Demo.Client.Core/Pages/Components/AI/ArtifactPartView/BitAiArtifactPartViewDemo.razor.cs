using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ArtifactPartView;

public partial class BitAiArtifactPartViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiArtifactPartViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the artifact part view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Conversation",
            Type = "BitAiConversation?",
            DefaultValue = "null",
            Description = "The conversation that holds the artifact, which the view finds it in by the id of the part (BitAiConversation.FindArtifact). While it is null, or holds no artifact of that id, the part is shown as a placeholder tag.",
            LinkType = LinkType.Link,
            Href = "/components/ai/messagelist/#conversation",
        },
        new()
        {
            Name = "Part",
            Type = "BitAiArtifactPart",
            DefaultValue = "null",
            Description = "The part of the message that references the artifact (required): its ArtifactId names it and its ArtifactVersion is the version the panel opens at, the latest when it is null.",
            LinkType = LinkType.Link,
            Href = "#artifact-part",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiArtifactPartViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the artifact part view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiArtifactPartViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiArtifactPartView."
                },
                new()
                {
                    Name = "ArtifactView",
                    Type = "BitAiArtifactViewClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiArtifactView the BitAiArtifactPartView shows the artifact in.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/artifactview/#class-styles"
                },
                new()
                {
                    Name = "Placeholder",
                    Type = "BitTagClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTag the BitAiArtifactPartView shows when the conversation does not hold the artifact.",
                    LinkType = LinkType.Link,
                    Href = "/components/tag/#tag-class-styles"
                },
            ]
        },
        new()
        {
            Id = "artifact-part",
            Title = "BitAiArtifactPart",
            Description = "The part of a message that says it introduced an artifact. It holds no content of its own - the artifact is the conversation's - only which artifact, and at which version. A session adds one to the message whose transport opened the artifact.",
            Parameters =
            [
                new()
                {
                    Name = "new(artifactId, partId?)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "Creates a reference to the artifact of that id; a null partId generates one."
                },
                new()
                {
                    Name = "ArtifactId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The id of the artifact in the conversation."
                },
                new()
                {
                    Name = "ArtifactVersion",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The version of the artifact this turn introduced, which the panel opens at, so an older turn keeps showing what it produced. Not to be confused with Version, which counts the changes of the part itself."
                },
            ]
        },
    ];



    // A conversation holding one artifact, the way a session leaves it after a turn that wrote one.
    private readonly BitAiConversation conversation = new();
    private readonly BitAiArtifactPart notesPart = new("art-notes");
    private readonly BitAiArtifactPart missingPart = new("art-deleted");
    private readonly BitAiArtifactPart persianPart = new("art-persian");

    // A scripted transport (Services/AI) that drafts a README into the artifact art-readme, and revises it on the next turn.
    private readonly BitAiChatSession readmeSession = new(AiDemoContentTransports.Artifacts());
    private BitAiRenderScheduler? renderScheduler;

    protected override void OnInitialized()
    {
        var notes = conversation.GetOrAddArtifact("art-notes", BitAiArtifactKind.Document, "Meeting notes");
        notes.SetContent("""
            ## Meeting notes

            - Ship the AI components in the next release.
            - **Owner**: the UI team.
            """);
        notes.Complete("First draft");

        var persian = conversation.GetOrAddArtifact("art-persian", BitAiArtifactKind.Document, "صورت جلسه");
        persian.SetContent("""
            ## صورت جلسه

            - انتشار اجزای هوش مصنوعی در نسخه بعدی.
            - **مسئول**: تیم رابط کاربری.
            """);
        persian.Complete();

        // The session says so on every streamed delta; the scheduler turns any number of those into one render per 50 ms.
        renderScheduler = new(() => InvokeAsync(StateHasChanged));
        readmeSession.StateChanged += renderScheduler.Request;
    }

    private Task SendAsync() => readmeSession.SubmitAsync(readmeSession.Conversation.Messages.Count == 0 ? "Write me a README" : "Tighten it and add an install step");

    public async ValueTask DisposeAsync()
    {
        if (renderScheduler is not null) await renderScheduler.DisposeAsync();

        await readmeSession.DisposeAsync();
    }
}
