namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Modal;

public partial class BitModalServiceDemo : IDisposable
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
            Name = "OpenModals",
            Type = "IReadOnlyList<BitModalReference>",
            DefaultValue = "",
            Description = "A snapshot of the modals this service currently has open, in the order they were opened. It holds what the mounted container renders, plus the persistent modals that are still waiting for a container to mount.",
        },
        new()
        {
            Name = "GetModal",
            Type = "BitModalReference? (string? id)",
            DefaultValue = "",
            Description = "The open modal with the given id, or null when there is none - it was closed, or the id belongs to another service.",
        },
        new()
        {
            Name = "Close",
            Type = "Task (BitModalReference modal)",
            DefaultValue = "",
            Description = "Closes an already opened modal using its reference, with a null result. This is the application closing the modal, so the CanClose guard is not asked.",
        },
        new()
        {
            Name = "Close",
            Type = "Task (BitModalReference modal, object? result)",
            DefaultValue = "",
            Description = "Closes an already opened modal using its reference, with the result its Result task completes with. The CanClose guard is not asked.",
        },
        new()
        {
            Name = "TryClose",
            Type = "Task<bool> (BitModalReference modal, object? result)",
            DefaultValue = "",
            Description = "Asks a modal to close and reports whether it did: a modal whose CanClose guard turns the close down stays open and this answers false.",
        },
        new()
        {
            Name = "CloseAll",
            Type = "Task",
            DefaultValue = "",
            Description = "Closes every modal this service currently has open, each with a null result. The CanClose guards are not asked.",
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
            Type = "Task<BitModalReference> (Type componentType, Dictionary<string, object>? parameters, BitModalParameters? modalParameters, bool persistent)",
            DefaultValue = "",
            Description = "Shows a new BitModal with a component whose type is only known at run time as its content, for the callers that pick their content from a map or a route. Throws an ArgumentException for a type that is not a Blazor component.",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitModalReference> (RenderFragment content, BitModalParameters? modalParameters, bool persistent)",
            DefaultValue = "",
            Description = "Shows a new BitModal with the given markup as its content, for the content that is not worth a component of its own. The reference's Content stays null for such a modal, since markup is not a component instance.",
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
            Description = "The handle a Show call hands back: what the modal is, what it answered with, and the ways to close it.",
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
                    Description = "The instance of the component rendered as the content of the modal. It is captured while the modal is rendered, which is after the Show call returns, so it is still null immediately afterwards."
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
                    Name = "IsDismissed",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the modal was closed by the user - the close button, the overlay, the Escape key - rather than by the application. It is what tells a modal that was walked away from apart from one answered with nothing, which the Result alone cannot."
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
                    Name = "Parameters",
                    Type = "BitModalParameters?",
                    DefaultValue = "null",
                    Description = "The parameters the modal is shown with, before they are merged with the container's own."
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
                    Name = "Rendered",
                    Type = "Task<bool>",
                    DefaultValue = "",
                    Description = "Completes with true once a container has rendered the modal, and with false for a modal that was closed before it ever rendered - one shown while no container was mounted, or closed in the same breath it was shown."
                },
                new()
                {
                    Name = "Close",
                    Type = "Task",
                    DefaultValue = "",
                    Description = "Closes the modal without a result. The CanClose guard is not asked."
                },
                new()
                {
                    Name = "CloseWith",
                    Type = "Task (object? result)",
                    DefaultValue = "",
                    Description = "Closes the modal with the given result, which is what its Result task completes with. The CanClose guard is not asked."
                },
                new()
                {
                    Name = "TryClose",
                    Type = "Task<bool> (object? result)",
                    DefaultValue = "",
                    Description = "Asks the modal to close and reports whether it did: a modal whose CanClose guard turns the close down stays open and this answers false."
                },
                new()
                {
                    Name = "Dismiss",
                    Type = "Task<bool>",
                    DefaultValue = "",
                    Description = "Closes the modal as a dismissal - the way the close button, the overlay and the Escape key close it - which asks the CanClose guard and marks the reference as dismissed. The content's own cancel action."
                },
                new()
                {
                    Name = "Update",
                    Type = "Task (BitModalParameters? parameters)",
                    DefaultValue = "",
                    Description = "Replaces the parameters the modal is shown with and re-renders it. The whole set is replaced rather than merged."
                },
                new()
                {
                    Name = "GetResult<T>",
                    Type = "Task<T?>",
                    DefaultValue = "",
                    Description = "The result the modal was closed with, cast to T - the type's default for a modal that was dismissed or answered with something else."
                },
                new()
                {
                    Name = "GetContentAsync<T>",
                    Type = "Task<T?>",
                    DefaultValue = "",
                    Description = "The component rendered as the content, cast to T, waiting for the modal to be rendered first. The type's default for a modal that never rendered or whose content is markup."
                }
            ]
        },
        new()
        {
            Id = "modal-parameters",
            Title = "BitModalParameters",
            Description = "The set of options a modal is shown with. Every parameter of the BitModal component has a nullable counterpart here (null meaning \"not set\", so the modal's own default or the container's value is used), plus the two options only a service can offer:",
            Parameters =
            [
                new()
                {
                    Name = "CanClose",
                    Type = "Func<Task<bool>>?",
                    DefaultValue = "null",
                    Description = "Asked before the user closes the modal - the close button, the overlay, the Escape key - and before an explicit TryClose. Answering false keeps the modal open. Close, CloseWith, CloseAll and a close on navigation are the application closing the modal and do not ask it. Only the guard on the modal's own parameters is asked, not one on the container's."
                },
                new()
                {
                    Name = "CloseOnNavigation",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Whether the modal closes when the app navigates somewhere else, which it does by default. Only a change of path counts; a query string or a fragment changed on the same page does not. Set it to false for the modals that outlive a route change."
                }
            ]
        }
    ];


    [AutoInject] private BitModalService modalService = default!;
    [AutoInject] private NavigationManager navigationManager = default!;


    protected override void OnInitialized()
    {
        modalService.OnAddModal += HandleOnAddModal;
        modalService.OnCloseModal += HandleOnCloseModal;

        base.OnInitialized();
    }


    private async Task ShowModal()
    {
        await modalService.Show<ModalContent>(new BitModalParameters() { FullWidth = true });
    }

    private async Task ShowChromeModal()
    {
        await modalService.Show<ModalBodyContent>(new BitModalParameters
        {
            MaxWidth = "32rem",
            HeaderText = "Shown by the service",
            ShowCloseButton = true,
            FooterText = "The footer of the modal."
        });
    }


    private string confirmAnswer = "-";
    private async Task ShowConfirmModal()
    {
        var modal = await modalService.Show<ConfirmModalContent>(new Dictionary<string, object>
        {
            { nameof(ConfirmModalContent.Question), "Delete the project?" }
        });

        var confirmed = await modal.GetResult<bool>();

        confirmAnswer = modal.IsDismissed ? "dismissed" : $"{confirmed}";

        StateHasChanged();
    }


    private string contentReport = "-";
    private async Task ShowContentReachingModal()
    {
        var modal = await modalService.Show<ConfirmModalContent>(new Dictionary<string, object>
        {
            { nameof(ConfirmModalContent.Question), "How long is this question?" }
        });

        // The content is only instantiated once the container renders the modal, so it is waited for rather
        // than read straight off the reference the Show call handed back.
        var content = await modal.GetContentAsync<ConfirmModalContent>();

        contentReport = $"{content?.Question?.Length ?? 0} characters";

        StateHasChanged();
    }


    private async Task ShowMarkupModal()
    {
        await modalService.Show(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "padding:1.5rem;max-width:26rem");
            builder.AddContent(2, "This modal was shown with markup rather than with a component of its own.");
            builder.CloseElement();
        });
    }


    private bool hasUnsavedChanges;
    private string guardReport = "-";
    private BitModalReference? guardedModal;
    private async Task ShowGuardedModal()
    {
        hasUnsavedChanges = false;

        guardedModal = await modalService.Show<UnsavedModalContent>(
            new Dictionary<string, object>
            {
                { nameof(UnsavedModalContent.HasChangesChanged), EventCallback.Factory.Create<bool>(this, v => hasUnsavedChanges = v) }
            },
            new BitModalParameters
            {
                ShowCloseButton = true,
                HeaderText = "Rename the project",
                CanClose = () => Task.FromResult(hasUnsavedChanges is false)
            });
    }

    private async Task TryCloseGuardedModal()
    {
        if (guardedModal is null || guardedModal.IsClosed)
        {
            guardReport = "nothing open";
            return;
        }

        guardReport = await guardedModal.TryClose() ? "closed" : "turned down";
    }


    private async Task ShowUpdatingModal()
    {
        var modal = await modalService.Show<ModalBodyContent>(new BitModalParameters
        {
            MaxWidth = "28rem",
            HeaderText = "Saving...",
            Blocking = true
        });

        // Standing in for the work: the modal blocks while it runs, and grows its way out once it is done.
        await Task.Delay(2000);

        await modal.Update(new BitModalParameters
        {
            MaxWidth = "28rem",
            HeaderText = "Saved",
            ShowCloseButton = true,
            FooterText = "The parameters were replaced while the modal was on the screen."
        });
    }


    private async Task ShowPersistentModal()
    {
        await modalService.Show<ModalContent>(new BitModalParameters { MaxWidth = "28rem" }, persistent: true);
    }


    private void NavigateWithQuery()
    {
        // The same page, so the modals on it are the modals of the page still being looked at.
        navigationManager.NavigateTo($"/components/modalservice?at={DateTime.Now.Ticks}#example9");
    }


    private async Task CloseAllModals()
    {
        await modalService.CloseAll();
    }


    private int shownCount;
    private int closedCount;
    private Task HandleOnAddModal(BitModalReference modalRef)
    {
        shownCount++;

        return InvokeAsync(StateHasChanged);
    }

    private Task HandleOnCloseModal(BitModalReference modalRef)
    {
        closedCount++;

        return InvokeAsync(StateHasChanged);
    }


    public void Dispose()
    {
        modalService.OnAddModal -= HandleOnAddModal;
        modalService.OnCloseModal -= HandleOnCloseModal;

        GC.SuppressFinalize(this);
    }


    private readonly string example1RazorCode = @"
<BitButton OnClick=""ShowModal"">Show</BitButton>

@* in the layout *@
<BitModalContainer />";
    private readonly string example1CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task ShowModal()
{
    await modalService.Show<ModalContent>(new BitModalParameters() { FullWidth = true });
}

// the same modal, with the content type only known at run time
private async Task ShowModalByType(Type contentType)
{
    await modalService.Show(contentType, modalParameters: new BitModalParameters() { FullWidth = true });
}";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""ShowChromeModal"">Show</BitButton>

<BitModalContainer />

@* ModalBodyContent.razor *@
<BitText>The header, the close button and the footer all come from the parameters the modal was shown with.</BitText>";
    private readonly string example2CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task ShowChromeModal()
{
    await modalService.Show<ModalBodyContent>(new BitModalParameters
    {
        MaxWidth = ""32rem"",
        HeaderText = ""Shown by the service"",
        ShowCloseButton = true,
        FooterText = ""The footer of the modal.""
    });
}";

    private readonly string example3RazorCode = @"
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
    private readonly string example3CsharpCode = @"
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

    var confirmed = await modal.GetResult<bool>();

    confirmAnswer = modal.IsDismissed ? ""dismissed"" : $""{confirmed}"";

    StateHasChanged();
}";

    private readonly string example4RazorCode = @"
<BitButton OnClick=""ShowContentReachingModal"">Show and count</BitButton>

<div>The content reported: [@contentReport]</div>";
    private readonly string example4CsharpCode = @"
private string contentReport = ""-"";

private async Task ShowContentReachingModal()
{
    var modal = await modalService.Show<ConfirmModalContent>(new Dictionary<string, object>
    {
        { nameof(ConfirmModalContent.Question), ""How long is this question?"" }
    });

    // The content is only instantiated once the container renders the modal, so it is waited for rather
    // than read straight off the reference the Show call handed back.
    var content = await modal.GetContentAsync<ConfirmModalContent>();

    contentReport = $""{content?.Question?.Length ?? 0} characters"";

    StateHasChanged();
}

// The other direction: the reference is handed to the factory before the content is built, so a
// parameter of the content can be a callback that closes this very modal.
private async Task ShowSelfClosingModal()
{
    await modalService.Show<UnsavedModalContent>(modalRef => new Dictionary<string, object>
    {
        { nameof(UnsavedModalContent.HasChangesChanged), EventCallback.Factory.Create<bool>(this, _ => modalRef.Close()) }
    });
}";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""ShowMarkupModal"">Show markup</BitButton>";
    private readonly string example5CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task ShowMarkupModal()
{
    await modalService.Show(builder =>
    {
        builder.OpenElement(0, ""div"");
        builder.AddAttribute(1, ""style"", ""padding:1.5rem;max-width:26rem"");
        builder.AddContent(2, ""This modal was shown with markup rather than with a component of its own."");
        builder.CloseElement();
    });
}";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""ShowGuardedModal"">Rename the project</BitButton>

<BitButton Variant=""BitVariant.Outline"" OnClick=""TryCloseGuardedModal"">TryClose it from here</BitButton>

<div>Last attempt: [@guardReport]</div>

@* UnsavedModalContent.razor *@
<BitStack Style=""padding:1rem;min-width:18rem"" Gap=""1rem"">
    <BitTextField Label=""Name"" Value=""@value"" ValueChanged=""OnValueChanged"" Immediate />
    <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
        <BitButton OnClick=""Save"">Save</BitButton>
        <BitButton Variant=""BitVariant.Outline"" OnClick=""Discard"">Discard</BitButton>
    </BitStack>
</BitStack>";
    private readonly string example6CsharpCode = @"
private bool hasUnsavedChanges;
private string guardReport = ""-"";
private BitModalReference? guardedModal;

private async Task ShowGuardedModal()
{
    hasUnsavedChanges = false;

    guardedModal = await modalService.Show<UnsavedModalContent>(
        new Dictionary<string, object>
        {
            { nameof(UnsavedModalContent.HasChangesChanged), EventCallback.Factory.Create<bool>(this, v => hasUnsavedChanges = v) }
        },
        new BitModalParameters
        {
            ShowCloseButton = true,
            HeaderText = ""Rename the project"",
            CanClose = () => Task.FromResult(hasUnsavedChanges is false)
        });
}

private async Task TryCloseGuardedModal()
{
    if (guardedModal is null || guardedModal.IsClosed)
    {
        guardReport = ""nothing open"";
        return;
    }

    guardReport = await guardedModal.TryClose() ? ""closed"" : ""turned down"";
}

// UnsavedModalContent.razor: the two ways out drop the changes first, so the guard lets the close through.
private async Task Discard()
{
    await HasChangesChanged.InvokeAsync(false);
    await modalReference.Dismiss();
}";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""ShowUpdatingModal"">Show, then update it</BitButton>";
    private readonly string example7CsharpCode = @"
private async Task ShowUpdatingModal()
{
    var modal = await modalService.Show<ModalBodyContent>(new BitModalParameters
    {
        MaxWidth = ""28rem"",
        HeaderText = ""Saving..."",
        Blocking = true
    });

    await SaveAsync();

    await modal.Update(new BitModalParameters
    {
        MaxWidth = ""28rem"",
        HeaderText = ""Saved"",
        ShowCloseButton = true,
        FooterText = ""The parameters were replaced while the modal was on the screen.""
    });
}

// mutating the parameters already handed to the modal works too, followed by a Refresh
private async Task RenameTheOpenModal(BitModalReference modal)
{
    modal.Parameters!.HeaderText = ""A new title"";

    await modalService.Refresh(modal);
}";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""ShowPersistentModal"">Show a persistent modal</BitButton>";
    private readonly string example8CsharpCode = @"
private async Task ShowPersistentModal()
{
    await modalService.Show<ModalContent>(new BitModalParameters { MaxWidth = ""28rem"" }, persistent: true);
}";

    private readonly string example9RazorCode = @"
<BitButton OnClick=""ShowModal"">Show a modal</BitButton>

<BitButton Variant=""BitVariant.Outline"" OnClick=""NavigateWithQuery"">Change the query string (it stays)</BitButton>

@* every modal of this container outlives the route change unless it says otherwise *@
<BitModalContainer ModalParameters=""new BitModalParameters { CloseOnNavigation = false }"" />";
    private readonly string example9CsharpCode = @"
[AutoInject] private NavigationManager navigationManager = default!;

private void NavigateWithQuery()
{
    // The same page, so the modals on it are the modals of the page still being looked at.
    navigationManager.NavigateTo($""/components/modalservice?at={DateTime.Now.Ticks}"");
}

// the modals that outlive a route change say so themselves
private async Task ShowLingeringModal()
{
    await modalService.Show<ModalContent>(new BitModalParameters { CloseOnNavigation = false });
}";

    private readonly string example10RazorCode = @"
<BitButton OnClick=""ShowModal"">Show one more</BitButton>

<div>Open: [@modalService.OpenModals.Count] &nbsp; Container mounted: [@modalService.IsContainerAvailable]</div>

<BitButton Variant=""BitVariant.Outline"" OnClick=""CloseAllModals"">Close all</BitButton>";
    private readonly string example10CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task CloseAllModals()
{
    await modalService.CloseAll();
}

// the code that only kept the id finds the modal again
private async Task CloseById(string id)
{
    var modal = modalService.GetModal(id);

    if (modal is not null)
    {
        await modal.Close();
    }
}";

    private readonly string example11RazorCode = @"
<BitButton OnClick=""ShowModal"">Show</BitButton>

<div>Shown: [@shownCount] &nbsp; Closed: [@closedCount]</div>";
    private readonly string example11CsharpCode = @"
private int shownCount;
private int closedCount;

protected override void OnInitialized()
{
    modalService.OnAddModal += HandleOnAddModal;
    modalService.OnCloseModal += HandleOnCloseModal;

    base.OnInitialized();
}

private Task HandleOnAddModal(BitModalReference modalRef)
{
    shownCount++;

    return InvokeAsync(StateHasChanged);
}

private Task HandleOnCloseModal(BitModalReference modalRef)
{
    closedCount++;

    return InvokeAsync(StateHasChanged);
}

public void Dispose()
{
    modalService.OnAddModal -= HandleOnAddModal;
    modalService.OnCloseModal -= HandleOnCloseModal;
}";
}
