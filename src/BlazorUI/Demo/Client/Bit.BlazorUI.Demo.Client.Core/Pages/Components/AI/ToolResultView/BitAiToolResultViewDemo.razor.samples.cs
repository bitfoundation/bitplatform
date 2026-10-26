namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ToolResultView;

public partial class BitAiToolResultViewDemo
{
    private readonly string example1RazorCode = """
<BitAiToolResultView Part="textResult" />
""";
    private readonly string example1CsharpCode = """"
private readonly BitAiToolResultPart textResult = new("call_logs", BitAiToolOutcome.FromText("""
    3 errors in the last hour:
    12:04  payment-api  Timeout after 30s calling the ledger
    12:31  payment-api  Timeout after 30s calling the ledger
    12:58  search       Index rebuild failed: out of disk space
    """))
{
    ToolName = "read_logs",
};
"""";

    private readonly string example2RazorCode = """
<BitAiToolResultView Part="jsonResult" />

<BitAiToolResultView Part="unnamedResult" />
""";
    private readonly string example2CsharpCode = """"
private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

private readonly BitAiToolResultPart jsonResult = new("call_weather", BitAiToolOutcome.Success(Json("""
    {"city":"Oslo","degrees":-3,"condition":"Snow","humidity":81,"forecast":[{"day":"Tue","high":-1},{"day":"Wed","high":2}]}
    """)))
{
    ToolName = "get_weather",
};

private readonly BitAiToolResultPart unnamedResult = new("call_unknown", BitAiToolOutcome.Success(Json("""{"deleted":128}""")));
"""";

    private readonly string example3RazorCode = """
<BitAiToolResultView Part="failedResult" />
""";
    private readonly string example3CsharpCode = """
private readonly BitAiToolResultPart failedResult = new("call_deploy", BitAiToolOutcome.Failure(new BitAiError("The deployment was rejected: the branch has failing checks.")
{
    Code = "checks_failed",
    Kind = BitAiErrorKind.Tool,
    Detail = "build (ubuntu-latest): failed\ntest (windows-latest): failed\nlint: passed",
}))
{
    ToolName = "deploy",
};
""";

    private readonly string example4RazorCode = """
@foreach (var part in resultMessage.Parts)
{
    <BitAiMessagePartView @key="part.PartId" Part="part" Message="resultMessage" />
}
""";
    private readonly string example4CsharpCode = """
private readonly BitAiMessage resultMessage = CreateResultMessage();

private static BitAiMessage CreateResultMessage()
{
    var message = new BitAiMessage(BitAiRole.Assistant);

    message.AddPart(new BitAiTextPart("I checked the logs of the last hour:"));
    message.AddPart(new BitAiToolResultPart("call_logs", BitAiToolOutcome.FromText("3 errors, 2 of them timeouts of payment-api.")) { ToolName = "read_logs" });
    message.AddPart(new BitAiTextPart("Both timeouts come from the ledger, so that is where to look first."));

    return message;
}
""";

    private readonly string example5RazorCode = """
<style>
    .custom-result {
        border-width: 2px;
        border-style: dashed;
    }

    .custom-result-header {
        color: #fff;
        background-color: #0b7a75;
    }

    .custom-result-body {
        padding: 1rem 1.5rem;
    }
</style>

<BitAiToolResultView Part="textResult" Style="box-shadow: 0 0.25rem 1rem #0003" />

<BitAiToolResultView Part="textResult" Class="custom-result" />

<BitAiToolResultView Part="textResult"
                     Styles="@(new() { Icon = "color: #0b7a75",
                                       Title = "font-style: italic",
                                       Text = "font-family: inherit" })" />

<BitAiToolResultView Part="textResult"
                     Classes="@(new() { Header = "custom-result-header",
                                        Body = "custom-result-body" })" />

<div style="--bit-AiToolResultView-background: var(--bit-clr-suc-tint); --bit-AiToolResultView-border-color: var(--bit-clr-suc); --bit-AiToolResultView-header-color: var(--bit-clr-suc-fg); --bit-AiToolResultView-body-background: transparent; --bit-AiToolResultView-radius: 1rem;">
    <BitAiToolResultView Part="jsonResult" />
</div>
""";

    private readonly string example6RazorCode = """
<BitAiToolResultView Dir="BitDir.Rtl" Part="rtlResult" />
""";
    private readonly string example6CsharpCode = """
private readonly BitAiToolResultPart rtlResult = new("call_weather_fa", BitAiToolOutcome.FromText("تهران: ۱۸ درجه، آفتابی\nرطوبت: ۲۰٪"))
{
    ToolName = "آب و هوا",
};
""";
}
