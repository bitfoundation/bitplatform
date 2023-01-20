namespace AdminPanel.Client.Shared;

public partial class LoadingComponent
{
    [Parameter] public string Color { get; set; } = "#123456";
    [Parameter] public bool AutoHide { get; set; }


    private bool _showLoading = true;

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender is false || AutoHide is false) return;

        _showLoading = false;
        StateHasChanged();
    }
}

