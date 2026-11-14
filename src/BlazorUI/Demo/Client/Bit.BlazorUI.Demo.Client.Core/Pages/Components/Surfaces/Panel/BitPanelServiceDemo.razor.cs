using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Panel;

public partial class BitPanelServiceDemo : IDisposable
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new() { Name = "OnAddModal", Type = "event Func<BitPanelReference, Task>?", DefaultValue = "", Description = "Raised for every panel shown through this service, whoever showed it." },
        new() { Name = "OnCloseModal", Type = "event Func<BitPanelReference, Task>?", DefaultValue = "", Description = "Raised once for every panel of this service that closes, however it closed." },
        new() { Name = "IsContainerAvailable", Type = "bool", DefaultValue = "", Description = "Whether a BitPanelContainer is currently mounted for this service, i.e. whether a Show call right now would render its panel." },
        new() { Name = "OpenModals", Type = "IReadOnlyList<BitPanelReference>", DefaultValue = "", Description = "A snapshot of the panels this service currently has open, in the order they were opened." },
        new() { Name = "GetModal", Type = "BitPanelReference? (string? id)", DefaultValue = "", Description = "The open panel with the given id, or null when there is none." },
        new() { Name = "Close", Type = "Task (BitPanelReference panel, object? result = null)", DefaultValue = "", Description = "Closes an open panel with the result its Result task completes with. This is the application closing the panel, so the CanClose guard is not asked." },
        new() { Name = "TryClose", Type = "Task<bool> (BitPanelReference panel, object? result)", DefaultValue = "", Description = "Asks a panel to close and reports whether it did: a panel whose CanClose guard turns the close down stays open and this answers false." },
        new() { Name = "CloseAll", Type = "Task", DefaultValue = "", Description = "Closes every panel this service currently has open, each with a null result, the last one opened first. The CanClose guards are not asked." },
        new() { Name = "Refresh", Type = "Task (BitPanelReference? panel)", DefaultValue = "", Description = "Re-renders the open panels, after their parameters were changed in place. Without an argument it refreshes every open panel." },
        new() { Name = "Show<T>", Type = "Task<BitPanelReference> (Dictionary<string, object>? parameters, BitPanelParameters? panelParameters, bool persistent)", DefaultValue = "", Description = "Shows a panel with the component T as its content, given the parameters. A persistent panel outlives the page it was opened from and a remount of its container. Overloads take the parameters as a BitModalContentParameters<T>, a factory handed the panel's reference, or no parameters at all." },
        new() { Name = "Show", Type = "Task<BitPanelReference> (Type componentType, ...)", DefaultValue = "", Description = "The same, with the type of the content known at run time." },
        new() { Name = "Show", Type = "Task<BitPanelReference> (RenderFragment content, BitPanelParameters? panelParameters, bool persistent)", DefaultValue = "", Description = "Shows a panel with markup as its content. An overload takes a RenderFragment<BitPanelReference>, handed the panel's own reference." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "panel-reference",
            Title = "BitPanelReference",
            Parameters =
            [
                new() { Name = "Id", Type = "string", DefaultValue = "", Description = "The unique id of the panel." },
                new() { Name = "Persistent", Type = "bool", DefaultValue = "", Description = "Whether the panel outlives the page it was opened from." },
                new() { Name = "IsClosed", Type = "bool", DefaultValue = "", Description = "Whether the panel has been closed." },
                new() { Name = "IsDismissed", Type = "bool", DefaultValue = "", Description = "Whether the panel was closed by the user rather than by the application: the overlay, the Escape key, the close button or a swipe." },
                new() { Name = "Content", Type = "object?", DefaultValue = "", Description = "The instance of the component rendered as the content, once it is rendered." },
                new() { Name = "Parameters", Type = "BitPanelParameters?", DefaultValue = "", Description = "The parameters the panel was shown with." },
                new() { Name = "Result", Type = "Task<object?>", DefaultValue = "", Description = "Completes with what the panel was closed with, or null." },
                new() { Name = "Rendered", Type = "Task<bool>", DefaultValue = "", Description = "Completes with true once a container has rendered the panel, or false when it is closed before that ever happens." },
                new() { Name = "Close / CloseWith", Type = "Task (object? result)", DefaultValue = "", Description = "Closes the panel, with no result or with the given one. The CanClose guard is not asked." },
                new() { Name = "TryClose", Type = "Task<bool> (object? result = null)", DefaultValue = "", Description = "Asks the panel to close, putting it to its CanClose guard first, and reports whether it did." },
                new() { Name = "Dismiss", Type = "Task<bool>", DefaultValue = "", Description = "Closes the panel as the user would, for the content's own cancel action: put to the guard, and reported as dismissed." },
                new() { Name = "Update", Type = "Task (BitPanelParameters? parameters) / Task (Action<BitPanelParameters> change)", DefaultValue = "", Description = "Replaces the parameters of the open panel, or changes the ones it names, and re-renders it." },
                new() { Name = "GetResult<T> / GetContentAsync<T>", Type = "Task<T?>", DefaultValue = "", Description = "The result as a T, and the content once it is rendered as a T." },
            ]
        },
        new()
        {
            Id = "panel-container",
            Title = "BitPanelContainer",
            Parameters =
            [
                new() { Name = "Service", Type = "BitPanelService?", DefaultValue = "null", Description = "The service whose panels this container renders, in place of the registered one." },
                new() { Name = "ModalParameters", Type = "BitPanelParameters", DefaultValue = "new()", Description = "The defaults of every panel the container renders: a panel's own parameters win over them, one by one." },
            ]
        },
        new()
        {
            Id = "panel-parameters",
            Title = "BitPanelParameters",
            Description = "The options a panel is shown with. Every parameter of BitPanel a panel shown from code can be given - including Class, Style, Dir, AriaLabel and Disabled, and its placement, size and snap points - has a nullable counterpart here (null means \"not set\": the container's value, then the panel's own default is used), plus the options only a service can offer:",
            Parameters =
            [
                new() { Name = "SnapPoint", Type = "int?", DefaultValue = "null", Description = "The snap point a bottom sheet opens at. The sheet keeps the one the user moves it to from there on." },
                new() { Name = "CanClose", Type = "Func<Task<bool>>?", DefaultValue = "null", Description = "The guard every dismissal by the user and every TryClose is put to: answering false keeps the panel open. Close is never put to it." },
                new() { Name = "CloseOnNavigation", Type = "bool?", DefaultValue = "null", Description = "Whether the panel closes when the app navigates somewhere else, which it does by default (a persistent panel only closes when this is set to true). Only a change of path counts; a query string or a fragment changed on the same page does not." },
            ]
        },
    ];



    [AutoInject] private BitPanelService panelService = default!;

    private async Task ShowPanel()
    {
        await panelService.Show<PanelContent>(new BitPanelParameters { HeaderText = "Shown by the service", ShowCloseButton = true });
    }

    private async Task ShowAt(BitPlacement placement)
    {
        await panelService.Show<PanelContent>(new BitPanelParameters
        {
            Placement = placement,
            Size = 320,
            HeaderText = $"From the {placement.ToString().ToLowerInvariant()}",
            ShowCloseButton = true,
        });
    }

    private async Task ShowSheet()
    {
        await panelService.Show<PanelContent>(new BitPanelParameters
        {
            Placement = BitPlacement.Bottom,
            SnapPoints = [0.3, 0.6, 0.9],
            SnapPoint = 1,
            HeaderText = "A bottom sheet",
        });
    }

    private string? pickedSize;

    private async Task PickSize()
    {
        var panel = await panelService.Show<PickPanelContent>(new BitPanelParameters { HeaderText = "Pick a size", ShowCloseButton = true });

        var size = await panel.GetResult<string>();

        pickedSize = panel.IsDismissed ? "(dismissed)" : size;
    }

    private async Task ShowMarkup()
    {
        // A template taking the panel's own reference, so the markup can close it.
        RenderFragment<BitPanelReference> content = panel => builder =>
        {
            builder.OpenComponent<BitButton>(0);
            builder.AddComponentParameter(1, nameof(BitButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => panel.Close()));
            builder.AddComponentParameter(2, nameof(BitButton.ChildContent), (RenderFragment)(b => b.AddContent(0, "Got it")));
            builder.CloseComponent();
        };

        await panelService.Show(content, new BitPanelParameters { HeaderText = "Markup that closes itself" });
    }

    private bool hasUnsavedChanges = true;

    private async Task ShowGuarded()
    {
        await panelService.Show<PanelContent>(new BitPanelParameters
        {
            HeaderText = "Guarded",
            ShowCloseButton = true,
            CanClose = () => Task.FromResult(hasUnsavedChanges is false),
        });
    }

    private CancellationTokenSource? countdownCts;

    private async Task ShowCountdown()
    {
        countdownCts?.Cancel();
        countdownCts = new CancellationTokenSource();
        var token = countdownCts.Token;

        var panel = await panelService.Show<PanelContent>(new BitPanelParameters { HeaderText = "Closing in 5", ShowCloseButton = true });

        for (var left = 4; left >= 0 && panel.IsClosed is false && token.IsCancellationRequested is false; left--)
        {
            try { await Task.Delay(1000, token); } catch (TaskCanceledException) { return; }

            if (left == 0)
            {
                await panel.Close();
            }
            else
            {
                await panel.Update(p => p.HeaderText = $"Closing in {left}");
            }
        }
    }

    public void Dispose()
    {
        countdownCts?.Cancel();
        countdownCts?.Dispose();
    }
}
