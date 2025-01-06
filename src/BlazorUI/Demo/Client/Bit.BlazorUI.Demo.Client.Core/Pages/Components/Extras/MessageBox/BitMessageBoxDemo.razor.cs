﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MessageBox;

public partial class BitMessageBoxDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "Body",
            Type = "string?",
            DefaultValue = "null",
            Description = "The body of the message box.",
         },
         new()
         {
            Name = "OkText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the Ok button.",
         },
         new()
         {
            Name = "OnClose",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The event callback for closing the message box.",
         },
         new()
         {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title of the message box.",
         },
    ];


    private bool isModalOpen;

    [AutoInject] private BitModalService modalService { get; set; } = default!;
    private async Task ShowMessageBox()
    {
        BitModalReference modalRef = default!;
        Dictionary<string, object> parameters = new()
        {
            { nameof(BitMessageBox.Title), "This is a title" },
            { nameof(BitMessageBox.Body), "This is a body." },
            { nameof(BitMessageBox.OnClose), EventCallback.Factory.Create(this, () => modalRef.Close()) }
        };
        modalRef = await modalService.Show<BitMessageBox>(parameters);
    }

    [AutoInject] private BitMessageBoxService messageBoxService { get; set; } = default!;
    private async Task ShowMessageBoxService()
    {
        await messageBoxService.Show("TITLE", "BODY");
    }



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
}
