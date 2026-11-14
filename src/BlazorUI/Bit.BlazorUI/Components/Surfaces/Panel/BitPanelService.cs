using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// Shows a <see cref="BitPanel"/> from code - a side panel, a top panel or a bottom sheet - with any component or markup
/// as its content, and hands back a <see cref="BitPanelReference"/> to close it and await what it was closed with.
/// </summary>
/// <remarks>
/// It is the <see cref="BitModalService"/> of panels, and works the same way: the panels are rendered by a
/// <see cref="BitPanelContainer"/> mounted once in the layout, in the same interactive render mode as the components that
/// show them; a persistent one outlives the page it was opened from; a dismissal is put to the
/// <see cref="BitPanelParameters.CanClose"/> guard; and a closed panel stays in the page until it has slid out.
/// </remarks>
public class BitPanelService : BitModalServiceBase<BitPanelReference, BitPanelParameters>
{
    internal const string MissingPanelContainerMessage =
        "No panel container is mounted, so the panel was not rendered. Mount a single panel container " +
        "(for example <BitPanelContainer />) in your layout, in the same interactive render mode as the " +
        "components that show panels.";



    public BitPanelService() : this(null)
    {
    }

    public BitPanelService(ILoggerFactory? loggerFactory) : base(loggerFactory)
    {
    }



    protected override string MissingContainerLogMessage => MissingPanelContainerMessage;

    protected override BitPanelReference CreateReference(bool persistent)
    {
        return new BitPanelReference(this, persistent);
    }

    // The guard a panel was shown with, which a panel with something to lose answers a dismissal with. Read off the
    // effective parameters, so a container can set the guard for every panel it renders and a single panel can still
    // say otherwise.
    protected override Func<Task<bool>>? GetCloseGuard(BitPanelReference panelReference)
    {
        return GetEffectiveParameters(panelReference)?.CanClose;
    }

    protected internal override BitPanelParameters CopyParameters(BitPanelParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return parameters.Copy();
    }

    protected override RenderFragment BuildModalFragment(BitPanelReference panelReference, RenderFragment content)
    {
        return new RenderFragment(builder =>
        {
            builder.OpenComponent<_BitPanelServiceHost>(0);
            builder.SetKey(panelReference.Id);
            builder.AddComponentParameter(1, nameof(_BitPanelServiceHost.Reference), panelReference);
            builder.AddComponentParameter(2, nameof(_BitPanelServiceHost.Content), content);
            builder.CloseComponent();
        });
    }
}
