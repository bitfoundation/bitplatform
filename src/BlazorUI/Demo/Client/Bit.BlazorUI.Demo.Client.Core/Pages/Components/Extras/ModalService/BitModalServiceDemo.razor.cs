namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ModalService;

public partial class BitModalServiceDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "OnAddModal",
            Type = "event Func<BitModalReference, Task>?",
            DefaultValue = "",
            Description = "The event for when a new modal gets added through calling the Show method.",
        },
        new()
        {
            Name = "OnCloseModal",
            Type = "event Func<BitModalReference, Task>?",
            DefaultValue = "",
            Description = "The event for when a modal gets removed through calling the Close method.",
        },
        new()
        {
            Name = "Close",
            Type = "void (BitModalReference modal)",
            DefaultValue = "",
            Description = "Closes an already opened modal using its reference.",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitModalReference> (Dictionary<string, object>? parameters)",
            DefaultValue = "",
            Description = "Shows a new BitModal with a custom component with parameters as its content.",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitModalReference> (BitModalParameters? modalParameters)",
            DefaultValue = "",
            Description = "Shows a new BitModal with a custom component as its content with custom parameters for the modal.",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitModalReference> (Dictionary<string, object>? parameters, BitModalParameters? modalParameters)",
            DefaultValue = "",
            Description = "Shows a new BitModal with a custom component as its content with custom parameters for the custom component and the modal.",
        },
    ];


    [AutoInject] private BitModalService modalService = default!;

    private async Task ShowModal()
    {
        await modalService.Show<ModalContent>(new BitModalParameters() { Modeless = true });
    }


    private readonly string example1RazorCode = @"
<BitButton OnClick=""ShowModal"">Show</BitButton>

<BitModalContainer ModalParameters=""@(new() { AutoToggleScroll = true, Blocking = true })"" />";
    private readonly string example1CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task ShowModal()
{
    await modalService.Show<ModalContent>(new BitModalParameters() { Modeless = true });
}";
}
