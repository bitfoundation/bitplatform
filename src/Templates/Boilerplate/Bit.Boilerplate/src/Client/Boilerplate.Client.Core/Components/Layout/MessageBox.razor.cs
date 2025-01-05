namespace Boilerplate.Client.Core.Components.Layout;

public partial class MessageBox
{
    [CascadingParameter] private BitModalReference? modalReference { get; set; }

    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Body { get; set; }


    private void CloseModal()
    {
        modalReference?.Close();
    }

    private void OnOkClick()
    {
        modalReference?.Close();
    }
}
