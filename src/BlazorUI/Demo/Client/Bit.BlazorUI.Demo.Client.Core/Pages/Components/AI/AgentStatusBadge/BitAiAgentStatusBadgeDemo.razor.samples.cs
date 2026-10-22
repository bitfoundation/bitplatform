namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.AgentStatusBadge;

public partial class BitAiAgentStatusBadgeDemo
{
    private readonly string example1RazorCode = """
<BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Running" />
""";

    private readonly string example2RazorCode = """
@foreach (var status in Enum.GetValues<BitAiAgentRunStatus>())
{
    <BitAiAgentStatusBadge Status="status" />
}
""";

    private readonly string example3RazorCode = """
<BitButton OnClick="FollowRun" Disabled="isFollowing">Run the agent</BitButton>

<BitAiAgentStatusBadge Status="followedStatus" />
""";
    private readonly string example3CsharpCode = """
private BitAiAgentRunStatus followedStatus = BitAiAgentRunStatus.Idle;
private bool isFollowing;

private async Task FollowRun()
{
    isFollowing = true;

    BitAiAgentRunStatus[] lifecycle =
    [
        BitAiAgentRunStatus.Planning,
        BitAiAgentRunStatus.Running,
        BitAiAgentRunStatus.WaitingForInput,
        BitAiAgentRunStatus.Running,
        BitAiAgentRunStatus.Completed,
    ];

    foreach (var status in lifecycle)
    {
        followedStatus = status;
        StateHasChanged();
        await Task.Delay(1500);
    }

    isFollowing = false;
}
""";

    private readonly string example4RazorCode = """
<BitAiOptionsProvider UiOptions="wordsOptions">
    @foreach (var status in Enum.GetValues<BitAiAgentRunStatus>())
    {
        <BitAiAgentStatusBadge Status="status" />
    }
</BitAiOptionsProvider>
""";
    private readonly string example4CsharpCode = """
private readonly BitAiUiOptions wordsOptions = BitAiUiOptions.Default with { Strings = new PlainStatusStrings() };

private sealed class PlainStatusStrings : BitAiStrings
{
    public override string AgentIdle => "Not started";
    public override string AgentPlanning => "Thinking it through";
    public override string AgentRunning => "Working on it";
    public override string WaitingForYou => "Needs you";
    public override string AgentCompleted => "Finished";
    public override string AgentFailed => "Could not finish";
    public override string AgentCancelled => "Cancelled";
}
""";

    private readonly string example5RazorCode = """
<BitParams Parameters="badgeParams">
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Running" />
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.WaitingForInput" />
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Completed"
                           Styles="@(new() { Tag = new() { Root = "border-radius: 0" } })" />
</BitParams>
""";
    private readonly string example5CsharpCode = """
private readonly BitAiAgentStatusBadgeParams[] badgeParams =
[
    new() { Styles = new() { Tag = new() { Root = "border-radius: 1rem", Text = "font-weight: 600" } } }
];
""";

    private readonly string example6RazorCode = """
<style>
    .custom-class {
        padding: 0.25rem;
        border-radius: 0.5rem;
        background-color: #dcfce7;
    }

    .custom-tag {
        box-shadow: 0 0 0.5rem #f59e0b;
    }
</style>

<BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Running" Style="padding: 0.25rem; outline: 1px dashed #0ea5e9" />
<BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Completed" Class="custom-class" />
<BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Failed"
                       Styles="@(new() { Tag = new() { Root = "border-radius: 1rem", Text = "font-style: italic" } })" />
<BitAiAgentStatusBadge Status="BitAiAgentRunStatus.WaitingForInput" Classes="@(new() { Tag = new() { Root = "custom-tag" } })" />

<div style="--bit-Tag-background: #312e81; --bit-Tag-color: #e0e7ff; --bit-Tag-font-weight: 600">
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Planning" />
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Running" />
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Completed" />
</div>
""";

    private readonly string example7RazorCode = """
<BitAiOptionsProvider UiOptions="rtlOptions">
    <div dir="rtl">
        <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Running" />
        <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.WaitingForInput" />
        <BitAiAgentStatusBadge Dir="BitDir.Rtl" Status="BitAiAgentRunStatus.Completed" />
    </div>
</BitAiOptionsProvider>
""";
    private readonly string example7CsharpCode = """
private readonly BitAiUiOptions rtlOptions = BitAiUiOptions.Default with { Strings = new PersianStatusStrings() };

private sealed class PersianStatusStrings : BitAiStrings
{
    public override string AgentRunning => "در حال اجرا";
    public override string WaitingForYou => "منتظر پاسخ شما";
    public override string AgentCompleted => "انجام شد";
}
""";
}
