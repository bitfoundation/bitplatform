namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Modal;

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
            Name = "IsContainerAvailable",
            Type = "bool",
            DefaultValue = "",
            Description = "Whether a BitModalContainer is currently mounted for this service, i.e. whether a Show call right now would actually render its modal. It reflects live state rather than registration in DI.",
        },
        new()
        {
            Name = "Close",
            Type = "Task (BitModalReference modal)",
            DefaultValue = "",
            Description = "Closes an already opened modal using its reference, with a null result.",
        },
        new()
        {
            Name = "Close",
            Type = "Task (BitModalReference modal, object? result)",
            DefaultValue = "",
            Description = "Closes an already opened modal using its reference, with the result its Result task completes with.",
        },
        new()
        {
            Name = "CloseAll",
            Type = "Task",
            DefaultValue = "",
            Description = "Closes every modal this service currently has open, each with a null result.",
        },
        new()
        {
            Name = "Refresh",
            Type = "Task (BitModalReference? modal)",
            DefaultValue = "",
            Description = "Re-renders the open modals, invalidating their memoized merged parameters. Call it after mutating modal parameters in place, which doesn't change any object reference and is therefore not detected on its own. Without an argument it refreshes every open modal.",
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
            Type = "Task<BitModalReference> (Dictionary<string, object>? parameters, BitModalParameters? modalParameters, bool persistent)",
            DefaultValue = "",
            Description = "Shows a new BitModal with a custom component as its content with custom parameters for the custom component and the modal. A persistent modal survives a container remount and is injected into the next container that mounts.",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitModalReference> (Func<BitModalReference, Dictionary<string, object>?> parametersFactory, BitModalParameters? modalParameters, bool persistent)",
            DefaultValue = "",
            Description = "Shows a new BitModal, building the content component's parameters from a factory that receives the modal reference. Use this overload when a parameter needs the reference itself, such as an OnClose callback that closes this very modal.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "modal-reference",
            Title = "BitModalReference",
            Parameters =
            [
                new()
                {
                    Name = "Id",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The unique id of the shown modal."
                },
                new()
                {
                    Name = "Content",
                    Type = "object?",
                    DefaultValue = "null",
                    Description = "The instance of the component rendered as the content of the modal."
                },
                new()
                {
                    Name = "IsClosed",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether this modal has already been closed. A reference is never reused, so once set it stays set."
                },
                new()
                {
                    Name = "Persistent",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the modal survives a container remount and is injected into the next container that mounts."
                },
                new()
                {
                    Name = "Result",
                    Type = "Task<object?>",
                    DefaultValue = "",
                    Description = "Completes when the modal is closed, with the value it was closed with - null for a modal that was dismissed rather than answered."
                },
                new()
                {
                    Name = "Close",
                    Type = "Task",
                    DefaultValue = "",
                    Description = "Closes the modal without a result."
                },
                new()
                {
                    Name = "CloseWith",
                    Type = "Task (object? result)",
                    DefaultValue = "",
                    Description = "Closes the modal with the given result, which is what its Result task completes with."
                }
            ]
        }
    ];


    [AutoInject] private BitModalService modalService = default!;

    private async Task ShowModal()
    {
        await modalService.Show<ModalContent>(new BitModalParameters() { FullWidth = true });
    }

    private string confirmAnswer = "-";
    private async Task ShowConfirmModal()
    {
        var modal = await modalService.Show<ConfirmModalContent>(new Dictionary<string, object>
        {
            { nameof(ConfirmModalContent.Question), "Delete the project?" }
        });

        var result = await modal.Result;

        confirmAnswer = result is null ? "dismissed" : $"{result}";

        StateHasChanged();
    }


    private readonly string example1RazorCode = @"
<BitButton OnClick=""ShowModal"">Show</BitButton>

<BitModalContainer />";
    private readonly string example1CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task ShowModal()
{
    await modalService.Show<ModalContent>(new BitModalParameters() { FullWidth = true });
}";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""ShowConfirmModal"">Delete the project</BitButton>

<div>Answer: [@confirmAnswer]</div>

@* ConfirmModalContent.razor *@
<BitStack Style=""padding:1rem"" Gap=""1rem"">
    <BitText Typography=""BitTypography.H6"">@Question</BitText>
    <BitSeparator />
    <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
        <BitButton OnClick=""() => modalReference.CloseWith(true)"">Yes</BitButton>
        <BitButton Variant=""BitVariant.Outline"" OnClick=""() => modalReference.CloseWith(false)"">No</BitButton>
    </BitStack>
</BitStack>";
    private readonly string example2CsharpCode = @"
// ConfirmModalContent.razor
[CascadingParameter] private BitModalReference modalReference { get; set; } = default!;

[Parameter] public string? Question { get; set; }

// the page
private string confirmAnswer = ""-"";

private async Task ShowConfirmModal()
{
    var modal = await modalService.Show<ConfirmModalContent>(new Dictionary<string, object>
    {
        { nameof(ConfirmModalContent.Question), ""Delete the project?"" }
    });

    var result = await modal.Result;

    confirmAnswer = result is null ? ""dismissed"" : $""{result}"";

    StateHasChanged();
}";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""ShowModal"">Show one more</BitButton>

<BitButton Variant=""BitVariant.Outline"" OnClick=""() => modalService.CloseAll()"">Close all</BitButton>";
    private readonly string example3CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task ShowModal()
{
    await modalService.Show<ModalContent>(new BitModalParameters() { FullWidth = true });
}";
}
