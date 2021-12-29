namespace TodoTemplate.App.Pages;

public partial class Index
{
    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {


        await base.OnInitializedAsync();
    }
}
