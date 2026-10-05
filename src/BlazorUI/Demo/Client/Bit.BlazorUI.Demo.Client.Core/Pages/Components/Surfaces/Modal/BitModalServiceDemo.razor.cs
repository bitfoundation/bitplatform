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
            Description = "Raised for every modal shown through this service, whoever showed it.",
        },
        new()
        {
            Name = "OnCloseModal",
            Type = "event Func<BitModalReference, Task>?",
            DefaultValue = "",
            Description = "Raised once for every modal of this service that closes, however it closed: answered or closed by the application, dismissed by the user, closed by a navigation or by its container going away.",
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
            Description = "Closes every modal this service currently has open, each with a null result, the last one opened first - so each hands the focus back to the modal under it, and the last to the page. The CanClose guards are not asked.",
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
            Type = "Task<BitModalReference> (RenderFragment<BitModalReference> content, BitModalParameters? modalParameters, bool persistent)",
            DefaultValue = "",
            Description = "Shows a new BitModal with markup built from the modal's own reference as its content, so the markup can close and answer the modal it is in: modal => @<BitButton OnClick=\"modal.Close\">Got it</BitButton>.",
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
                    Name = "Update",
                    Type = "Task (Action<BitModalParameters> change)",
                    DefaultValue = "",
                    Description = "Changes some of the parameters and re-renders the modal, leaving the others as they are: modal.Update(p => p.HeaderText = \"Step 2\"). The change is applied to a copy, so a set shared between showings is never changed under the other modals."
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
            Id = "modal-container",
            Title = "BitModalContainer",
            Description = "The component that renders the modals of the service. Mount one, in the layout.",
            Parameters =
            [
                new()
                {
                    Name = "ModalParameters",
                    Type = "BitModalParameters",
                    DefaultValue = "new()",
                    LinkType = LinkType.Link,
                    Href = "#modal-parameters",
                    Description = "The defaults of every modal this container renders - the house style: a maximum width, a close button, a position. The parameters of one showing win over them, and they win over a BitModalParams cascaded by a BitParams."
                },
                new()
                {
                    Name = "Service",
                    Type = "BitModalService?",
                    DefaultValue = "null",
                    Description = "The service this container renders the modals of, in place of the one registered in DI - for a region with modals of its own. Read when the container initializes; give the container a @key to switch it."
                }
            ]
        },
        new()
        {
            Id = "modal-parameters",
            Title = "BitModalParameters",
            Description = "The options a modal is shown with. Every parameter of BitModal - including Class, Style, Dir, AriaLabel and Disabled - has a nullable counterpart here (null means \"not set\": the container's value, then a BitParams default, then the modal's own default is used), plus the two options only a service can offer:",
            Parameters =
            [
                new()
                {
                    Name = "CanClose",
                    Type = "Func<Task<bool>>?",
                    DefaultValue = "null",
                    Description = "Asked before the user closes the modal - the close button, the overlay, the Escape key - and by TryClose and Dismiss. Answering false keeps the modal open. Close, CloseWith, CloseAll and a close on navigation are the application closing the modal and do not ask it. A guard on the container's parameters is the default for every modal; one of the modal's own is asked instead."
                },
                new()
                {
                    Name = "CloseOnNavigation",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Whether the modal closes when the app navigates somewhere else, which it does by default (a persistent modal only closes when this is set to true). Only a change of path counts; a query string or a fragment changed on the same page does not. Set it to false for the modals that outlive a route change."
                }
            ]
        },
        new()
        {
            Id = "modal-content-parameters",
            Title = "BitModalContentParameters<TComponent>",
            Description = "The parameters of the content component, named by its own properties instead of strings. It is a Dictionary<string, object>, so it goes wherever a Show overload takes the content's parameters.",
            Parameters =
            [
                new()
                {
                    Name = "Add<TValue>",
                    Type = "void (Expression<Func<TComponent, TValue>> parameter, TValue value)",
                    DefaultValue = "",
                    Description = "Adds the value of one parameter, named as c => c.Parameter - the collection initializer form is { c => c.Parameter, value }. Throws an ArgumentException for an expression that names no [Parameter] property of TComponent, or a value its type cannot take."
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

        // The persistent example's own service: the ordinary modal is closed by the container that unmounts
        // rather than by anything on this page, so the lines reporting the two modals are re-rendered from here.
        demoModalService.OnCloseModal += HandleOnDemoModalClose;

        base.OnInitialized();
    }


    private async Task ShowModal()
    {
        await modalService.Show<ModalContent>(new BitModalParameters { AriaLabel = "Hello from the service" });
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


    private async Task ShowMarkupModal()
    {
        await modalService.Show(builder => builder.AddContent(0, "This modal was shown with markup rather than with a component of its own."),
                                new BitModalParameters { MaxWidth = "28rem", HeaderText = "Markup", ShowCloseButton = true });
    }


    private string confirmAnswer = "-";
    private async Task ShowConfirmModal()
    {
        var modal = await modalService.Show<ConfirmModalContent>(new BitModalContentParameters<ConfirmModalContent>
        {
            { c => c.Question, "Delete the project?" }
        }, new BitModalParameters { AriaLabel = "Delete the project?", IsAlert = true });

        var confirmed = await modal.GetResult<bool>();

        confirmAnswer = modal.IsDismissed ? "dismissed" : $"{confirmed}";

        StateHasChanged();
    }


    private string contentReport = "-";
    private async Task ShowContentReachingModal()
    {
        var modal = await modalService.Show<ConfirmModalContent>(new BitModalContentParameters<ConfirmModalContent>
        {
            { c => c.Question, "How long is this question?" }
        }, new BitModalParameters { AriaLabel = "How long is this question?" });

        // The content is only instantiated once the container renders the modal, so it is waited for rather
        // than read straight off the reference the Show call handed back.
        var content = await modal.GetContentAsync<ConfirmModalContent>();

        contentReport = $"{content?.Question?.Length ?? 0} characters";

        StateHasChanged();
    }


    private bool hasUnsavedChanges;
    private string guardReport = "-";
    private BitModalReference? guardedModal;
    private async Task ShowGuardedModal()
    {
        hasUnsavedChanges = false;
        guardReport = "-";

        guardedModal = await modalService.Show<UnsavedModalContent>(
            new BitModalContentParameters<UnsavedModalContent>
            {
                { c => c.HasChangesChanged, EventCallback.Factory.Create<bool>(this, v => hasUnsavedChanges = v) }
            },
            new BitModalParameters
            {
                // Modeless only so the TryClose button of the page stays reachable while the modal is open;
                // the guard is asked the same on a modal that holds the page.
                Modeless = true,
                ShowCloseButton = true,
                HeaderText = "Rename the project",
                CanClose = GuardTheClose
            });
    }

    // The guard reports what it answered, so a dismissal it turns down - which leaves the modal exactly where
    // it was - is visible as something having happened rather than as a click that did nothing.
    private Task<bool> GuardTheClose()
    {
        var canClose = hasUnsavedChanges is false;

        guardReport = canClose ? "let through" : "turned down (unsaved change)";
        StateHasChanged();

        return Task.FromResult(canClose);
    }

    private async Task TryCloseGuardedModal()
    {
        if (guardedModal is null || guardedModal.IsClosed)
        {
            guardReport = "nothing open";
            return;
        }

        await guardedModal.TryClose();
    }


    private async Task ShowUpdatingModal()
    {
        var modal = await modalService.Show<ModalBodyContent>(new BitModalParameters
        {
            MaxWidth = "28rem",
            HeaderText = "Saving...",
            Blocking = true,
            NoDismissOnEscape = true
        });

        // Standing in for the work: the modal can't be dismissed while it runs, and gets its way out once done.
        await Task.Delay(2000);

        // Only what changes is named: the rest of the set stays as it was shown.
        await modal.Update(p =>
        {
            p.HeaderText = "Saved";
            p.Blocking = null;
            p.NoDismissOnEscape = null;
            p.ShowCloseButton = true;
            p.FooterText = "Only what changed was named; the rest of the set stayed.";
        });
    }


    // A service of the example's own, so the container below can be unmounted without taking the modals of
    // the rest of the page - which the layout's BitModalContainer renders - down with it. An app has one
    // service and one container; two of each is what an example that unmounts a container costs.
    private readonly BitModalService demoModalService = new();
    private bool isDemoContainerMounted = true;
    private BitModalReference? persistentModal;
    private BitModalReference? ordinaryModal;

    // Both are shown Modeless and in a corner of their own so the buttons of the page stay reachable while they
    // are open, which is what the example is for.
    private async Task ShowPersistentModal()
    {
        persistentModal = await demoModalService.Show<ModalBodyContent>(
            new BitModalParameters { MaxWidth = "24rem", HeaderText = "Persistent", ShowCloseButton = true, Modeless = true, Position = BitPosition.TopStart },
            persistent: true);
    }

    private async Task ShowOrdinaryModal()
    {
        ordinaryModal = await demoModalService.Show<ModalBodyContent>(
            new BitModalParameters { MaxWidth = "24rem", HeaderText = "Ordinary", ShowCloseButton = true, Modeless = true, Position = BitPosition.TopEnd });
    }

    // Unmounting the container is what tells the two apart: the ordinary modal is closed by the container that
    // was rendering it, and the persistent one is only taken off the screen until a container mounts again.
    private void ToggleDemoContainer()
    {
        isDemoContainerMounted = isDemoContainerMounted is false;
    }

    private string DescribeModal(BitModalReference? modalRef)
    {
        if (modalRef is null) return "never shown";

        if (modalRef.IsClosed) return "closed";

        return isDemoContainerMounted ? "open" : "open, waiting for a container";
    }


    private BitModalReference? navigationModal;
    private BitModalReference? lingeringModal;

    // Both are shown Modeless so the navigation buttons of the page stay reachable while they are open, which
    // is what the example is for; a modal that holds the page behaves the same way on a route change.
    private async Task ShowNavigationModal()
    {
        navigationModal = await modalService.Show<ModalBodyContent>(new BitModalParameters
        {
            MaxWidth = "24rem",
            Modeless = true,
            ShowCloseButton = true,
            Position = BitPosition.TopStart,
            HeaderText = "Closes on navigation"
        });
    }

    // The modals that outlive a route change say so themselves.
    private async Task ShowLingeringModal()
    {
        lingeringModal = await modalService.Show<ModalBodyContent>(new BitModalParameters
        {
            MaxWidth = "24rem",
            Modeless = true,
            ShowCloseButton = true,
            Position = BitPosition.TopEnd,
            HeaderText = "Stays across a route change",
            CloseOnNavigation = false
        });
    }

    private void NavigateWithQuery()
    {
        // The same page, so the modals on it are the modals of the page still being looked at.
        navigationManager.NavigateTo($"/components/modalservice?at={DateTime.Now.Ticks}#example8");
    }

    private void NavigateToAnotherPage()
    {
        // A different path, which is what closes the modals of the page being left behind.
        navigationManager.NavigateTo("/components/modal");
    }

    private static string DescribeNavigationModal(BitModalReference? modalRef)
    {
        if (modalRef is null) return "never shown";

        return modalRef.IsClosed ? "closed" : "open";
    }


    private int shownCount;
    private int closedCount;

    // Shown Modeless and in a corner so the buttons of the page stay reachable and every modal of the stack stays
    // visible, which is what makes the counters and the CloseAll next to them something to watch.
    private async Task ShowStackedModal()
    {
        var count = modalService.OpenModals.Count;

        await modalService.Show<ModalBodyContent>(new BitModalParameters
        {
            MaxWidth = "20rem",
            Modeless = true,
            ShowCloseButton = true,
            HeaderText = $"Modal {count + 1}",
            Position = StackedModalPosition(count)
        });
    }

    private static BitPosition StackedModalPosition(int index) => (index % 5) switch
    {
        0 => BitPosition.TopStart,
        1 => BitPosition.TopEnd,
        2 => BitPosition.BottomStart,
        3 => BitPosition.BottomEnd,
        _ => BitPosition.Center
    };

    private async Task CloseAllModals()
    {
        await modalService.CloseAll();
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


    // The cascading example renders through a service and a container of its own, like the persistent one, so the
    // BitParams around its container reaches only the modals it shows rather than every modal of the page.
    private readonly BitModalService paramsModalService = new();

    private readonly BitModalParams[] modalParams =
    [
        new()
        {
            ModeFull = true,
            ShowCloseButton = true,
            MaxWidth = "26rem",
            Position = BitPosition.TopCenter,
        }
    ];

    private async Task ShowCascadedModal()
    {
        await paramsModalService.Show<ModalBodyContent>(new BitModalParameters { HeaderText = "Cascaded" });
    }

    private async Task ShowCascadedOwnModal()
    {
        await paramsModalService.Show<ModalBodyContent>(new BitModalParameters
        {
            HeaderText = "Own position",
            Position = BitPosition.BottomCenter
        });
    }


    private async Task ShowExternalIconModal()
    {
        await modalService.Show<ModalBodyContent>(new BitModalParameters
        {
            MaxWidth = "32rem",
            ShowCloseButton = true,
            HeaderText = "External close icon",
            CloseIcon = BitIconInfo.Fa("solid xmark")
        });
    }


    private async Task ShowCssVariablesModal()
    {
        await modalService.Show<ModalBodyContent>(new BitModalParameters
        {
            ModeFull = true,
            ShowCloseButton = true,
            Position = BitPosition.TopCenter,
            HeaderText = "CSS variables",
            FooterText = "Restyled without a single class.",
            Style = "--bit-Modal-background: #1e1b4b; --bit-Modal-color: #e0e7ff; --bit-Modal-border-color: #a5b4fc; --bit-Modal-radius: 1rem; --bit-Modal-padding: 1.5rem; --bit-Modal-offset: 2rem; --bit-Modal-max-width: 30rem; --bit-Modal-overlay-background: #1e1b4b99; --bit-Modal-overlay-backdrop-filter: blur(4px);"
        });
    }

    private async Task ShowStyledPartsModal()
    {
        await modalService.Show<ModalBodyContent>(new BitModalParameters
        {
            MaxWidth = "32rem",
            ShowCloseButton = true,
            HeaderText = "Styled parts",
            Styles = new()
            {
                Overlay = "background-color: #4776f433;",
                Content = "box-shadow: 0 0 1rem tomato;",
                Header = "color: tomato;"
            }
        });
    }


    private async Task ShowRtlModal()
    {
        await modalService.Show(builder => builder.AddContent(0, "لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است."),
                                new BitModalParameters
                                {
                                    Dir = BitDir.Rtl,
                                    MaxWidth = "30rem",
                                    ShowCloseButton = true,
                                    Position = BitPosition.TopStart,
                                    HeaderText = "لورم ایپسوم",
                                    CloseButtonTitle = "بستن"
                                });
    }


    private Task HandleOnDemoModalClose(BitModalReference modalRef)
    {
        return InvokeAsync(StateHasChanged);
    }


    public void Dispose()
    {
        modalService.OnAddModal -= HandleOnAddModal;
        modalService.OnCloseModal -= HandleOnCloseModal;

        demoModalService.OnCloseModal -= HandleOnDemoModalClose;

        GC.SuppressFinalize(this);
    }
}
