namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ProModalService;

public partial class BitProModalServiceDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "OnAddModal",
            Type = "event Func<BitProModalReference, Task>?",
            DefaultValue = "",
            Description = "The event for when a new modal gets added through calling the Show method.",
        },
        new()
        {
            Name = "OnCloseModal",
            Type = "event Func<BitProModalReference, Task>?",
            DefaultValue = "",
            Description = "The event for when a modal gets removed through calling the Close method.",
        },
        new()
        {
            Name = "Close",
            Type = "Task (BitProModalReference modal)",
            DefaultValue = "",
            Description = "Closes an already opened modal using its reference.",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitProModalReference> (Dictionary<string, object>? parameters)",
            DefaultValue = "",
            Description = "Shows a new BitProModal with a custom component with parameters as its content.",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitProModalReference> (BitProModalParameters? modalParameters)",
            DefaultValue = "",
            Description = "Shows a new BitProModal with a custom component as its content with custom parameters for the modal.",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitProModalReference> (Dictionary<string, object>? parameters, BitProModalParameters? modalParameters)",
            DefaultValue = "",
            Description = "Shows a new BitProModal with a custom component as its content with custom parameters for the custom component and the modal.",
        },
    ];


    [AutoInject] private BitProModalService proModalService = default!;

    private async Task ShowModal()
    {
        await proModalService.Show<ProModalContent>(new BitProModalParameters()
        {
            HeaderText = "BitProModalService",
            ShowCloseButton = true
        });
    }


    private readonly string example1RazorCode = @"
<BitButton OnClick=""ShowModal"">Show</BitButton>

<BitProModalContainer />";
    private readonly string example1CsharpCode = @"
[AutoInject] private BitProModalService proModalService = default!;

private async Task ShowModal()
{
    await proModalService.Show<ProModalContent>(new BitProModalParameters()
    {
        HeaderText = ""BitProModalService"",
        ShowCloseButton = true
    });
}";
}
