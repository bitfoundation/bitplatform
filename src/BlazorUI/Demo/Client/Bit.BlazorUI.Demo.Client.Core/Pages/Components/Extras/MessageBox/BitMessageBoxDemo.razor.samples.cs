namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MessageBox;

public partial class BitMessageBoxDemo
{
    private readonly string example1RazorCode = @"
<BitCard Style=""padding:0"">
    <BitMessageBox Title=""It's a title"" Body=""It's a body."" />
</BitCard>";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => isModalOpen = true"">Show</BitButton>
<BitModal @bind-IsOpen=""isModalOpen"">
    <BitMessageBox OnClose=""() => isModalOpen = false"" Title=""This is the Title"" Body=""This is the Body!"" />
</BitModal>";
    private readonly string example2CsharpCode = @"
private bool isModalOpen;";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""ShowMessageBox"">Show MessageBox</BitButton>

<BitModalContainer />";
    private readonly string example3CsharpCode = @"
[AutoInject] private BitModalService modalService { get; set; } = default!;
private async Task ShowMessageBox()
{
    BitModalReference modalRef = default!;
    Dictionary<string, object> parameters = new()
    {
        { nameof(BitMessageBox.Title), ""This is a title"" },
        { nameof(BitMessageBox.Body), ""This is a body."" },
        { nameof(BitMessageBox.OnClose), EventCallback.Factory.Create(this, () => modalRef.Close()) }
    };
    modalRef = await modalService.Show<BitMessageBox>(parameters);
}";

    private readonly string example4RazorCode = @"
<BitButton OnClick=""ShowMessageBoxService"">Show MessageBox</BitButton>";
    private readonly string example4CsharpCode = @"
[AutoInject] private BitMessageBoxService messageBoxService { get; set; } = default!;
private async Task ShowMessageBoxService()
{
    await messageBoxService.Show(""TITLE"", ""BODY"");
}";

    private readonly string example5RazorCode = @"
<style>
    .custom-msg {
        background: linear-gradient(180deg, #3e0f0f, transparent) #000;
    }

    .custom-msg-btn {
        color: #fff;
        font-weight: bold;
        border-radius: 1rem;
        border-color: #8f0101;
        transition: background-color 1s;
        background: linear-gradient(90deg, #d10000, transparent) #8f0101;
    }

    .custom-msg-btn:hover {
        color: #fff;
        font-weight: bold;
        border-color: #8f0101;
        background-color: #8f0101;
    }
</style>

<BitCard Style=""padding:0"">
    <BitMessageBox Title=""It's a title""
                    Body=""It's a body.""
                    Styles=""@(new() { Root = ""background: linear-gradient(180deg, #222444, transparent) #000"", OkButton = new() { Root = ""border-radius:1rem"" } })"" />
</BitCard>

<BitCard Style=""padding:0"">
    <BitMessageBox Title=""It's a title""
                    Body=""It's a body.""
                    Classes=""@(new() { Root = ""custom-msg"", OkButton = new() { Root = ""custom-msg-btn"" } })"" />
</BitCard>";

    private readonly string example6RazorCode = @"
<BitCard Style=""padding:0"">
    <BitMessageBox Dir=""BitDir.Rtl"" Title=""عنوان پیام"" Body=""متن تست پیام..."" OkText=""تایید"" />
</BitCard>";
}
