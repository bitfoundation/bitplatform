namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.AgentStatusBadge;

public partial class BitAiAgentStatusBadgeDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiAgentStatusBadgeClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the agent status badge.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Status",
            Type = "BitAiAgentRunStatus",
            DefaultValue = "BitAiAgentRunStatus.Idle",
            Description = "The status to describe (required): the word comes from the string table of the options in force, and the color of the tag follows it - the default look of a tag for Planning and Running, the warning color for WaitingForInput, success for Completed, error for Failed and the neutral (tertiary) one for Idle and Cancelled.",
            LinkType = LinkType.Link,
            Href = "#agent-run-status-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiAgentStatusBadgeClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the agent status badge.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiAgentStatusBadgeClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element (the live region) of the BitAiAgentStatusBadge." },
                new()
                {
                    Name = "Tag",
                    Type = "BitTagClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTag of the BitAiAgentStatusBadge.",
                    LinkType = LinkType.Link,
                    Href = "/components/tag#tag-class-styles",
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "agent-run-status-enum",
            Name = "BitAiAgentRunStatus",
            Description = "The lifecycle of a whole agent run (BitAiAgentRun.Status), and what a BitAiAgentStatusBadge says for each.",
            Items =
            [
                new() { Name = "Idle", Description = "Created but not started. \"Idle\" (BitAiStrings.AgentIdle), neutral.", Value = "0" },
                new() { Name = "Planning", Description = "Deciding what to do before producing visible output. \"Planning\" (AgentPlanning), in the default look of a tag.", Value = "1" },
                new() { Name = "Running", Description = "Executing steps. \"Running\" (AgentRunning), in the default look of a tag.", Value = "2" },
                new() { Name = "WaitingForInput", Description = "Waiting on the user: an approval, a question, or missing input. \"Waiting for your answer\" (WaitingForYou), in the warning color.", Value = "3" },
                new() { Name = "Completed", Description = "Completed successfully. \"Done\" (AgentCompleted), in the success color.", Value = "4" },
                new() { Name = "Failed", Description = "Ended with an error. \"Failed\" (AgentFailed), in the error color.", Value = "5" },
                new() { Name = "Cancelled", Description = "Cancelled before completion. \"Stopped\" (AgentCancelled), neutral.", Value = "6" },
            ]
        },
    ];



    // Following a run: the status of a run moving through its lifecycle.
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



    // Words: another tone, through a subclass of the string table.
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



    // Cascading parameters.
    private readonly BitAiAgentStatusBadgeParams[] badgeParams =
    [
        new() { Styles = new() { Tag = new() { Root = "border-radius: 1rem", Text = "font-weight: 600" } } }
    ];



    // RTL: the words of a right-to-left language.
    private readonly BitAiUiOptions rtlOptions = BitAiUiOptions.Default with { Strings = new PersianStatusStrings() };

    private sealed class PersianStatusStrings : BitAiStrings
    {
        public override string AgentRunning => "در حال اجرا";
        public override string WaitingForYou => "منتظر پاسخ شما";
        public override string AgentCompleted => "انجام شد";
    }
}
