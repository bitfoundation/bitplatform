namespace Boilerplate.Workbench.Components.Pages.Showcase.Assistant;

public partial class AssistantHistory
{
    [Parameter] public IReadOnlyList<AssistantConversation> Conversations { get; set; } = [];

    [Parameter] public AssistantConversation? Current { get; set; }

    [Parameter] public bool IsLoading { get; set; }

    [Parameter] public bool IsFailed { get; set; }

    [Parameter] public EventCallback<AssistantConversation> OnSelect { get; set; }

    [Parameter] public EventCallback OnNewChat { get; set; }

    private string FormatDate(DateTimeOffset value)
    {
        var days = (DateTimeOffset.Now.Date - value.Date).Days;

        return days switch
        {
            0 => Localizer[nameof(WorkbenchStrings.AssistantToday)],
            1 => Localizer[nameof(WorkbenchStrings.AssistantYesterday)],
            _ => value.ToString("d MMM", CultureInfo.CurrentCulture)
        };
    }
}
