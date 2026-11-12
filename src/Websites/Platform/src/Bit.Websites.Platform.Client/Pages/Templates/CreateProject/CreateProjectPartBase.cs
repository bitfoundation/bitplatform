namespace Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

public partial class CreateProjectPartBase : AppComponentBase
{
    [CascadingParameter] public CreateProjectState? PageState { get; set; }

    protected CreateProjectState State => PageState ?? throw new InvalidOperationException("A part of the create project page is shown outside the page.");

    protected CreateProjectAnswers Answers => State.Answers;

    protected override async Task OnInitAsync()
    {
        State.Changed += OnStateChanged;

        await base.OnInitAsync();
    }

    private void OnStateChanged() => _ = InvokeAsync(StateHasChanged);

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (PageState is not null)
        {
            PageState.Changed -= OnStateChanged;
        }

        await base.DisposeAsync(disposing);
    }
}
