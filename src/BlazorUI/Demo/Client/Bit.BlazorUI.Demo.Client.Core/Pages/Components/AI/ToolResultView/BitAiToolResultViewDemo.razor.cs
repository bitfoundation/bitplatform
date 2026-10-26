using System.Text.Json;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ToolResultView;

public partial class BitAiToolResultViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiToolResultViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the tool result view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Part",
            Type = "BitAiToolResultPart",
            DefaultValue = "",
            Description = "The result to render (required). The card is headed by its ToolName (else by the Result text of the BitAiStrings in force) and shows its Outcome: a failure as a compact BitAiErrorView, a Text as it was written, else the Value as a BitAiJsonView.",
            LinkType = LinkType.Link,
            Href = "#tool-result-part",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiToolResultViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the tool result view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiToolResultViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiToolResultView.",
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitAiToolResultView.",
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the tool icon of the BitAiToolResultView.",
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitAiToolResultView.",
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of the BitAiToolResultView.",
                },
                new()
                {
                    Name = "Text",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for a textual result of the BitAiToolResultView.",
                },
            ],
        },
        new()
        {
            Id = "tool-result-part",
            Title = "BitAiToolResultPart",
            Description = "A tool result carried on its own (Kind \"tool-result\") rather than on the BitAiToolCallPart it answers. The stream processor never adds one: it gives a result to the call of its id, holding one that arrives ahead of its call for ToolResultHoldTime and dropping one whose call never starts. An app adds one to a message it builds itself - a conversation imported from a store that keeps each result as a message of its own.",
            Parameters =
            [
                new()
                {
                    Name = "BitAiToolResultPart(toolCallId, outcome, partId?)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "Creates a result of the call identified by toolCallId; partId is generated when left out.",
                },
                new()
                {
                    Name = "ToolCallId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The call the result answers.",
                },
                new()
                {
                    Name = "ToolName",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The tool that produced the result, when known. It heads the card.",
                },
                new()
                {
                    Name = "Outcome",
                    Type = "BitAiToolOutcome",
                    DefaultValue = "",
                    Description = "The result itself: its Value, Text or Error.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/toolcallview#tool-outcome",
                },
                new()
                {
                    Name = "PartId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier of the part (inherited from BitAiMessagePart, like the four members below): the partId given to the constructor, else a generated one.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/messagepartview#message-part",
                },
                new()
                {
                    Name = "Kind",
                    Type = "string",
                    DefaultValue = "\"tool-result\"",
                    Description = "The kind of the part (BitAiMessagePartKinds.ToolResult), which BitAiMessagePartView renders with this view.",
                },
                new()
                {
                    Name = "Version",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "Increments on every change of the part.",
                },
                new()
                {
                    Name = "Message",
                    Type = "BitAiMessage?",
                    DefaultValue = "null",
                    Description = "The message the part belongs to, once it has been added to one.",
                },
                new()
                {
                    Name = "Changed",
                    Type = "event Action<BitAiMessagePart>",
                    DefaultValue = "",
                    Description = "Raised after a part changes - which a result part never does: its Outcome is fixed, and setting its ToolName raises nothing. The view does not listen to it, so a ToolName set afterwards is drawn with the next render of the page.",
                },
            ],
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiToolResultView-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Surface of the card." },
        new() { Name = "--bit-AiToolResultView-border-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Outline of the card, and the rule under its header." },
        new() { Name = "--bit-AiToolResultView-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the card." },
        new() { Name = "--bit-AiToolResultView-header-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text and icon of the header." },
        new() { Name = "--bit-AiToolResultView-body-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Surface of the body." },
    ];



    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    private readonly BitAiToolResultPart textResult = new("call_logs", BitAiToolOutcome.FromText("""
        3 errors in the last hour:
        12:04  payment-api  Timeout after 30s calling the ledger
        12:31  payment-api  Timeout after 30s calling the ledger
        12:58  search       Index rebuild failed: out of disk space
        """))
    {
        ToolName = "read_logs",
    };



    private readonly BitAiToolResultPart jsonResult = new("call_weather", BitAiToolOutcome.Success(Json("""
        {"city":"Oslo","degrees":-3,"condition":"Snow","humidity":81,"forecast":[{"day":"Tue","high":-1},{"day":"Wed","high":2}]}
        """)))
    {
        ToolName = "get_weather",
    };

    private readonly BitAiToolResultPart unnamedResult = new("call_unknown", BitAiToolOutcome.Success(Json("""{"deleted":128}""")));



    private readonly BitAiToolResultPart failedResult = new("call_deploy", BitAiToolOutcome.Failure(new BitAiError("The deployment was rejected: the branch has failing checks.")
    {
        Code = "checks_failed",
        Kind = BitAiErrorKind.Tool,
        Detail = "build (ubuntu-latest): failed\ntest (windows-latest): failed\nlint: passed",
    }))
    {
        ToolName = "deploy",
    };



    private readonly BitAiMessage resultMessage = CreateResultMessage();

    private static BitAiMessage CreateResultMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        message.AddPart(new BitAiTextPart("I checked the logs of the last hour:"));
        message.AddPart(new BitAiToolResultPart("call_logs", BitAiToolOutcome.FromText("3 errors, 2 of them timeouts of payment-api.")) { ToolName = "read_logs" });
        message.AddPart(new BitAiTextPart("Both timeouts come from the ledger, so that is where to look first."));

        return message;
    }



    private readonly BitAiToolResultViewParams[] resultViewParams =
    [
        new() { Styles = new() { Root = "border-style: dashed", Title = "font-style: italic" } },
    ];



    private readonly BitAiToolResultPart rtlResult = new("call_weather_fa", BitAiToolOutcome.FromText("تهران: ۱۸ درجه، آفتابی\nرطوبت: ۲۰٪"))
    {
        ToolName = "آب و هوا",
    };

    private readonly BitAiToolResultPart rtlUnnamedResult = new("call_unknown_fa", BitAiToolOutcome.FromText("۱۲۸ رکورد حذف شد."));

    private readonly BitAiUiOptions persianOptions = BitAiUiOptions.Default with { Strings = new PersianAiStrings() };

    private sealed class PersianAiStrings : BitAiStrings
    {
        public override string Result => "نتیجه";
    }
}
