namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Modal;

public partial class BitModalServiceDemo
{
    private const string modalContentCode = @"
<BitStack Style=""padding:1rem"" Gap=""1rem"">
    <BitText Typography=""BitTypography.H6"">Hello from the service</BitText>
    <BitText>This content reaches its own modal through the cascaded BitModalReference.</BitText>
    <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
        <BitButton OnClick=""() => modalReference.Close()"">Ok</BitButton>
        <BitButton Variant=""BitVariant.Outline"" OnClick=""() => modalReference.Dismiss()"">Cancel</BitButton>
    </BitStack>
</BitStack>

@code {
    [CascadingParameter] private BitModalReference modalReference { get; set; } = default!;
}";

    private const string modalBodyContentCode = @"
<BitText>This content knows nothing of the modal around it: the chrome comes from the parameters it was shown with.</BitText>";

    private const string confirmModalContentCode = @"
<BitStack Style=""padding:1rem"" Gap=""1rem"">
    <BitText Typography=""BitTypography.H6"">@Question</BitText>
    <BitSeparator />
    <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
        <BitButton OnClick=""() => modalReference.CloseWith(true)"">Yes</BitButton>
        @* the least destructive answer takes the focus when the modal opens *@
        <BitButton AutoFocus Variant=""BitVariant.Outline"" OnClick=""() => modalReference.CloseWith(false)"">No</BitButton>
    </BitStack>
</BitStack>

@code {
    [CascadingParameter] private BitModalReference modalReference { get; set; } = default!;

    [Parameter] public string? Question { get; set; }
}";

    private const string unsavedModalContentCode = @"
<BitStack Style=""padding:1rem;min-width:18rem"" Gap=""1rem"">
    <BitTextField Label=""Name"" Value=""@value"" ValueChanged=""OnValueChanged"" Immediate />
    @if (hasChanges)
    {
        <BitMessage Color=""BitColor.Warning"">
            Unsaved change: the close button, Escape and TryClose are turned down until you save or discard.
        </BitMessage>
    }
    <BitStack Horizontal Gap=""0.5rem"" AutoHeight>
        <BitButton OnClick=""Save"">Save</BitButton>
        <BitButton Variant=""BitVariant.Outline"" OnClick=""Discard"">Discard</BitButton>
    </BitStack>
</BitStack>

@code {
    [CascadingParameter] private BitModalReference modalReference { get; set; } = default!;

    [Parameter] public EventCallback<bool> HasChangesChanged { get; set; }

    private string? value;
    private bool hasChanges;

    private async Task OnValueChanged(string? newValue)
    {
        value = newValue;
        hasChanges = true;

        await HasChangesChanged.InvokeAsync(true);
    }

    private async Task Save()
    {
        hasChanges = false;

        await HasChangesChanged.InvokeAsync(false);
        await modalReference.CloseWith(value);
    }

    // Discarding is the way out the guard leaves open: the changes are dropped first, so the close goes through.
    private async Task Discard()
    {
        hasChanges = false;

        await HasChangesChanged.InvokeAsync(false);
        await modalReference.Dismiss();
    }
}";

    private readonly string example1RazorCode = @"
@* in the layout *@
<BitModalContainer />

<BitButton OnClick=""ShowModal"">Show</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowChromeModal"">With header &amp; footer</BitButton>";
    private readonly string example1CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task ShowModal()
{
    // no header to name the dialog, so it is given an accessible name of its own
    await modalService.Show<ModalContent>(new BitModalParameters { AriaLabel = ""Hello from the service"" });
}

private async Task ShowChromeModal()
{
    await modalService.Show<ModalBodyContent>(new BitModalParameters
    {
        MaxWidth = ""32rem"",
        HeaderText = ""Shown by the service"",
        ShowCloseButton = true,
        FooterText = ""The footer of the modal.""
    });
}

// the same modal, with the content type only known at run time
private async Task ShowModalByType(Type contentType)
{
    await modalService.Show(contentType, modalParameters: new BitModalParameters { AriaLabel = ""Hello from the service"" });
}";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("ModalContent.razor", modalContentCode),
        new("ModalBodyContent.razor", modalBodyContentCode),
    ];

    private readonly string example2RazorCode = @"
<BitButton OnClick=""ShowMarkupModal"">Show markup</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowClosingMarkupModal"">Markup that closes itself</BitButton>

@code {
    // a template taking the modal's own reference, so the markup can close it
    private async Task ShowClosingMarkupModal()
    {
        await modalService.Show(modal => @<BitStack Gap=""1rem"" Style=""padding:1rem"">
            <BitText>This markup was handed the reference of its own modal.</BitText>
            <BitButton OnClick=""modal.Close"">Got it</BitButton>
        </BitStack>, new BitModalParameters { AriaLabel = ""Markup that closes itself"" });
    }
}";
    private readonly string example2CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task ShowMarkupModal()
{
    await modalService.Show(builder => builder.AddContent(0, ""This modal was shown with markup rather than with a component of its own.""),
                            new BitModalParameters { MaxWidth = ""28rem"", HeaderText = ""Markup"", ShowCloseButton = true });
}

// in a .razor file, markup is written as a template
private async Task ShowTemplateModal()
{
    await modalService.Show(@<BitText>A line of text, shown as it is.</BitText>);
}";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""ShowConfirmModal"">Delete the project</BitButton>
<div>Answer: [@confirmAnswer]</div>";
    private readonly string example3CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private string confirmAnswer = ""-"";

private async Task ShowConfirmModal()
{
    // the content's parameters, checked by the compiler; a Dictionary<string, object> works too
    var modal = await modalService.Show<ConfirmModalContent>(new BitModalContentParameters<ConfirmModalContent>
    {
        { c => c.Question, ""Delete the project?"" }
    }, new BitModalParameters { AriaLabel = ""Delete the project?"", IsAlert = true });

    var confirmed = await modal.GetResult<bool>();

    confirmAnswer = modal.IsDismissed ? ""dismissed"" : $""{confirmed}"";

    StateHasChanged();
}";

    private readonly DemoCodeFile[] example3CodeFiles =
    [
        new("ConfirmModalContent.razor", confirmModalContentCode),
    ];

    private readonly string example4RazorCode = @"
<BitButton OnClick=""ShowContentReachingModal"">Show and count</BitButton>
<div>The content reported: [@contentReport]</div>";
    private readonly string example4CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private string contentReport = ""-"";

private async Task ShowContentReachingModal()
{
    var modal = await modalService.Show<ConfirmModalContent>(new BitModalContentParameters<ConfirmModalContent>
    {
        { c => c.Question, ""How long is this question?"" }
    }, new BitModalParameters { AriaLabel = ""How long is this question?"" });

    // The content is only instantiated once the container renders the modal, so it is waited for rather
    // than read straight off the reference the Show call handed back.
    var content = await modal.GetContentAsync<ConfirmModalContent>();

    contentReport = $""{content?.Question?.Length ?? 0} characters"";

    StateHasChanged();
}

// The other direction: the reference is handed to the factory before the content is built, so a
// parameter of the content can be built from it - a callback that closes this very modal, or its id.
private async Task ShowSelfNamingModal()
{
    await modalService.Show<ConfirmModalContent>(modalRef => new BitModalContentParameters<ConfirmModalContent>
    {
        { c => c.Question, $""Close modal {modalRef.Id}?"" }
    }, new BitModalParameters { AriaLabel = ""Close the modal?"" });
}";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""ShowGuardedModal"">Rename the project</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""TryCloseGuardedModal"">TryClose it from here</BitButton>
<div>Last attempt: [@guardReport]</div>";
    private readonly string example5CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private bool hasUnsavedChanges;
private string guardReport = ""-"";
private BitModalReference? guardedModal;

private async Task ShowGuardedModal()
{
    hasUnsavedChanges = false;
    guardReport = ""-"";

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
            HeaderText = ""Rename the project"",
            CanClose = GuardTheClose
        });
}

// The guard reports what it answered, so a dismissal it turns down is visible as something having happened.
private Task<bool> GuardTheClose()
{
    var canClose = hasUnsavedChanges is false;

    guardReport = canClose ? ""let through"" : ""turned down (unsaved change)"";
    StateHasChanged();

    return Task.FromResult(canClose);
}

private async Task TryCloseGuardedModal()
{
    if (guardedModal is null || guardedModal.IsClosed)
    {
        guardReport = ""nothing open"";
        return;
    }

    await guardedModal.TryClose();
}";

    private readonly DemoCodeFile[] example5CodeFiles =
    [
        new("UnsavedModalContent.razor", unsavedModalContentCode),
    ];

    private readonly string example6RazorCode = @"
<BitButton OnClick=""ShowUpdatingModal"">Show, then update it</BitButton>";
    private readonly string example6CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task ShowUpdatingModal()
{
    var modal = await modalService.Show<ModalBodyContent>(new BitModalParameters
    {
        MaxWidth = ""28rem"",
        HeaderText = ""Saving..."",
        Blocking = true,
        NoDismissOnEscape = true
    });

    // Standing in for the work: the modal can't be dismissed while it runs, and gets its way out once done.
    await Task.Delay(2000);

    // only what changes is named: the rest of the set stays as it was shown
    await modal.Update(p =>
    {
        p.HeaderText = ""Saved"";
        p.Blocking = null;
        p.NoDismissOnEscape = null;
        p.ShowCloseButton = true;
        p.FooterText = ""Only what changed was named; the rest of the set stayed."";
    });
}

// a whole set replaces the one the modal was shown with
private async Task ReplaceTheParameters(BitModalReference modal)
{
    await modal.Update(new BitModalParameters { HeaderText = ""A new set"", ShowCloseButton = true });
}

// mutating the parameters already handed to the modal works too, followed by a Refresh
private async Task RenameTheOpenModal(BitModalReference modal)
{
    modal.Parameters!.HeaderText = ""A new title"";

    await modalService.Refresh(modal);
}";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""ShowPersistentModal"">Show a persistent modal</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowOrdinaryModal"">Show an ordinary one</BitButton>
<BitButton Variant=""BitVariant.Text"" OnClick=""ToggleDemoContainer"">
    @(isDemoContainerMounted ? ""Unmount the container"" : ""Mount the container again"")
</BitButton>
<div>Container: [@(isDemoContainerMounted ? ""mounted"" : ""unmounted"")]</div>
<div>Persistent modal: [@DescribeModal(persistentModal)]</div>
<div>Ordinary modal: [@DescribeModal(ordinaryModal)]</div>

@* An app mounts one container, in its layout: this one renders a service of the example's own,
   so that unmounting it leaves the modals of the rest of the page alone. *@
@if (isDemoContainerMounted)
{
    <BitModalContainer Service=""demoModalService"" />
}";
    private readonly string example7CsharpCode = @"
private readonly BitModalService demoModalService = new();
private bool isDemoContainerMounted = true;
private BitModalReference? persistentModal;
private BitModalReference? ordinaryModal;

private async Task ShowPersistentModal()
{
    persistentModal = await demoModalService.Show<ModalBodyContent>(
        new BitModalParameters { MaxWidth = ""24rem"", HeaderText = ""Persistent"", ShowCloseButton = true, Modeless = true, Position = BitPosition.TopStart },
        persistent: true);
}

private async Task ShowOrdinaryModal()
{
    ordinaryModal = await demoModalService.Show<ModalBodyContent>(
        new BitModalParameters { MaxWidth = ""24rem"", HeaderText = ""Ordinary"", ShowCloseButton = true, Modeless = true, Position = BitPosition.TopEnd });
}

// Unmounting the container is what tells the two apart: the ordinary modal is closed by the container that
// was rendering it, and the persistent one is only taken off the screen until a container mounts again.
private void ToggleDemoContainer()
{
    isDemoContainerMounted = isDemoContainerMounted is false;
}

private string DescribeModal(BitModalReference? modalRef)
{
    if (modalRef is null) return ""never shown"";

    if (modalRef.IsClosed) return ""closed"";

    return isDemoContainerMounted ? ""open"" : ""open, waiting for a container"";
}";

    private readonly string example8RazorCode = @"
@* in the layout: every modal of this container outlives a route change unless it says otherwise
<BitModalContainer ModalParameters=""new BitModalParameters { CloseOnNavigation = false }"" /> *@

<BitButton OnClick=""ShowNavigationModal"">Show a modal</BitButton>
<BitButton OnClick=""ShowLingeringModal"">Show one that stays</BitButton>

<BitButton Variant=""BitVariant.Outline"" OnClick=""NavigateWithQuery"">Change the query string (both stay)</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""NavigateToAnotherPage"">Go to another page</BitButton>

<div>Modal: [@DescribeNavigationModal(navigationModal)]</div>
<div>Modal that stays: [@DescribeNavigationModal(lingeringModal)]</div>";
    private readonly string example8CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;
[AutoInject] private NavigationManager navigationManager = default!;

private BitModalReference? navigationModal;
private BitModalReference? lingeringModal;

private async Task ShowNavigationModal()
{
    navigationModal = await modalService.Show<ModalBodyContent>(new BitModalParameters
    {
        MaxWidth = ""24rem"",
        Modeless = true,
        ShowCloseButton = true,
        Position = BitPosition.TopStart,
        HeaderText = ""Closes on navigation""
    });
}

// the modals that outlive a route change say so themselves
private async Task ShowLingeringModal()
{
    lingeringModal = await modalService.Show<ModalBodyContent>(new BitModalParameters
    {
        MaxWidth = ""24rem"",
        Modeless = true,
        ShowCloseButton = true,
        Position = BitPosition.TopEnd,
        HeaderText = ""Stays across a route change"",
        CloseOnNavigation = false
    });
}

private void NavigateWithQuery()
{
    // The same page, so the modals on it are the modals of the page still being looked at.
    navigationManager.NavigateTo($""/components/modalservice?at={DateTime.Now.Ticks}#example8"");
}

private void NavigateToAnotherPage()
{
    // A different path, which is what closes the modals of the page being left behind.
    navigationManager.NavigateTo(""/components/modal"");
}

private static string DescribeNavigationModal(BitModalReference? modalRef)
{
    if (modalRef is null) return ""never shown"";

    return modalRef.IsClosed ? ""closed"" : ""open"";
}";

    private readonly string example9RazorCode = @"
@implements IDisposable

<BitButton OnClick=""ShowStackedModal"">Show one more</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""CloseAllModals"">Close all</BitButton>
<div>
    Open: [@modalService.OpenModals.Count] &nbsp; Shown: [@shownCount] &nbsp; Closed: [@closedCount]
    &nbsp; Container mounted: [@modalService.IsContainerAvailable]
</div>";
    private readonly string example9CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private int shownCount;
private int closedCount;

protected override void OnInitialized()
{
    modalService.OnAddModal += HandleOnAddModal;
    modalService.OnCloseModal += HandleOnCloseModal;

    base.OnInitialized();
}

// shown Modeless and in a corner so the page stays reachable and every modal of the stack stays visible
private async Task ShowStackedModal()
{
    var count = modalService.OpenModals.Count;

    await modalService.Show<ModalBodyContent>(new BitModalParameters
    {
        MaxWidth = ""20rem"",
        Modeless = true,
        ShowCloseButton = true,
        HeaderText = $""Modal {count + 1}"",
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

// the code that only kept the id finds the modal again
private async Task CloseById(string id)
{
    var modal = modalService.GetModal(id);

    if (modal is not null)
    {
        await modal.Close();
    }
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

    private readonly string example10RazorCode = @"
@* in an app, around the container in the layout:
<BitParams Parameters=""modalParams"">
    <BitModalContainer />
</BitParams> *@

<BitButton OnClick=""ShowCascadedModal"">Takes the cascade</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowCascadedOwnModal"">Its own position</BitButton>

@* a container rendering a service of the example's own, so the cascade reaches only its modals *@
<BitParams Parameters=""modalParams"">
    <BitModalContainer Service=""paramsModalService"" />
</BitParams>";
    private readonly string example10CsharpCode = @"
private readonly BitModalService paramsModalService = new();

private readonly BitModalParams[] modalParams =
[
    new()
    {
        ModeFull = true,
        ShowCloseButton = true,
        MaxWidth = ""26rem"",
        Position = BitPosition.TopCenter,
    }
];

private async Task ShowCascadedModal()
{
    await paramsModalService.Show<ModalBodyContent>(new BitModalParameters { HeaderText = ""Cascaded"" });
}

private async Task ShowCascadedOwnModal()
{
    await paramsModalService.Show<ModalBodyContent>(new BitModalParameters
    {
        HeaderText = ""Own position"",
        Position = BitPosition.BottomCenter
    });
}";

    private readonly string example11RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitButton OnClick=""ShowExternalIconModal"">External close icon</BitButton>";
    private readonly string example11CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task ShowExternalIconModal()
{
    await modalService.Show<ModalBodyContent>(new BitModalParameters
    {
        MaxWidth = ""32rem"",
        ShowCloseButton = true,
        HeaderText = ""External close icon"",
        CloseIcon = BitIconInfo.Fa(""solid xmark"")
    });
}";

    private readonly string example12RazorCode = @"
@* in the layout: the house style of every modal
<BitModalContainer ModalParameters=""houseStyle"" /> *@

<BitButton OnClick=""ShowCssVariablesModal"">CSS variables</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowStyledPartsModal"">Styles</BitButton>";
    private readonly string example12CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

// in the layout
private readonly BitModalParameters houseStyle = new() { Style = ""--bit-Modal-radius: 1rem"" };

private async Task ShowCssVariablesModal()
{
    await modalService.Show<ModalBodyContent>(new BitModalParameters
    {
        ModeFull = true,
        ShowCloseButton = true,
        Position = BitPosition.TopCenter,
        HeaderText = ""CSS variables"",
        FooterText = ""Restyled without a single class."",
        Style = ""--bit-Modal-background: #1e1b4b; --bit-Modal-color: #e0e7ff; --bit-Modal-border-color: #a5b4fc; --bit-Modal-radius: 1rem; --bit-Modal-padding: 1.5rem; --bit-Modal-offset: 2rem; --bit-Modal-max-width: 30rem; --bit-Modal-overlay-background: #1e1b4b99; --bit-Modal-overlay-backdrop-filter: blur(4px);""
    });
}

private async Task ShowStyledPartsModal()
{
    await modalService.Show<ModalBodyContent>(new BitModalParameters
    {
        MaxWidth = ""32rem"",
        ShowCloseButton = true,
        HeaderText = ""Styled parts"",
        Styles = new()
        {
            Overlay = ""background-color: #4776f433;"",
            Content = ""box-shadow: 0 0 1rem tomato;"",
            Header = ""color: tomato;""
        }
    });
}";

    private readonly string example13RazorCode = @"
<div dir=""rtl"">
    <BitButton Dir=""BitDir.Rtl"" OnClick=""ShowRtlModal"">باز کردن مُدال</BitButton>
</div>";
    private readonly string example13CsharpCode = @"
[AutoInject] private BitModalService modalService = default!;

private async Task ShowRtlModal()
{
    await modalService.Show(builder => builder.AddContent(0, ""لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.""),
                            new BitModalParameters
                            {
                                Dir = BitDir.Rtl,
                                MaxWidth = ""30rem"",
                                ShowCloseButton = true,
                                Position = BitPosition.TopStart,
                                HeaderText = ""لورم ایپسوم"",
                                CloseButtonTitle = ""بستن""
                            });
}";
}
