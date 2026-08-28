using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// A core service to show any content inside a centralized <see cref="BitModal"/> using <see cref="BitModalContainer"/>.
/// </summary>
/// <remarks>
/// A <see cref="BitModalContainer"/> must be mounted in the layout for shown modals to render: a non-persistent modal
/// shown while no container is mounted is silently not rendered (see the base type remarks) and reported through the
/// logger factory, where one is registered. Use
/// <see cref="BitModalServiceBase{TReference, TParameters}.IsContainerAvailable"/> to check whether a container is
/// currently mounted before showing a modal.
/// </remarks>
public class BitModalService : BitModalServiceBase<BitModalReference, BitModalParameters>
{
    public BitModalService() : this(null)
    {
    }

    public BitModalService(ILoggerFactory? loggerFactory) : base(loggerFactory)
    {
    }



    protected override BitModalReference CreateReference(bool persistent)
    {
        return new BitModalReference(this, persistent);
    }

    // The guard a modal was shown with, which is what a modal with something to lose answers a dismissal with.
    // Read off the effective parameters, so a container can set the guard for every modal it renders and a
    // single modal can still say otherwise.
    protected override Func<Task<bool>>? GetCloseGuard(BitModalReference modalReference)
    {
        return GetEffectiveParameters(modalReference)?.CanClose;
    }

    protected override RenderFragment BuildModalFragment(BitModalReference modalReference, RenderFragment content)
    {
        return new RenderFragment(builder =>
        {
            var seq = 0;
            builder.OpenComponent<BitModal>(seq++);
            builder.SetKey(modalReference.Id);
            builder.AddComponentParameter(seq++, nameof(BitModal.IsOpen), true);
            // Dismissal is wired through IsOpenChanged rather than OnOverlayClick so that the
            // Blocking parameter is honored. IsOpenChanged only fires after BitModal's own
            // AssignIsOpen succeeds, and BitModal short-circuits (without calling AssignIsOpen)
            // when Blocking is set, so a Blocking modal won't light-dismiss on overlay click.
            // Wiring through OnOverlayClick instead would call Close() before BitModal's Blocking
            // guard runs, bypassing Blocking.
            //
            // It goes through Dismiss rather than Close so that the close guard gets its say and the
            // reference records that the user was the one who closed the modal.
            builder.AddComponentParameter(seq++, nameof(BitModal.IsOpenChanged), EventCallback.Factory.Create<bool>(modalReference, () => modalReference.Dismiss()));
            builder.AddComponentParameter(seq++, nameof(BitModal.ChildContent), content);
            builder.CloseComponent();
        });
    }
}
