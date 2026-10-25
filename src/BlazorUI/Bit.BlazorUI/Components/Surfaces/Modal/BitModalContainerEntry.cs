using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// One modal of a container, with everything it expects from its container cascaded to it: its reference, what
/// it reports its exit through (<see cref="BitModalExit"/>) and its merged parameters.
/// </summary>
/// <remarks>
/// It renders only when something of its own modal changed - the merged parameters, a refresh, the modal closing -
/// and not on every render of the container. A cascaded value of a class type is handed again to everything
/// reading it whenever the cascade renders, and with it the previous parameters of each of those components, a
/// moment before their parent hands them the current ones: rendered with the container, every modal and every
/// content reading its reference would go through that each time another modal is shown or closed. Rendered only
/// when its own modal changed, the cascades still hand the content its reference again then, so content reading
/// the state of the reference (its parameters, <c>IsClosed</c>) stays current.
/// </remarks>
internal sealed class BitModalContainerEntry<TReference, TParameters> : IComponent
    where TReference : BitModalReferenceBase<TReference, TParameters>
    where TParameters : class, new()
{
    internal const string ReferenceName = "Reference";
    internal const string ParametersName = "Parameters";
    internal const string ExitName = "Exit";
    internal const string VersionName = "Version";

    private readonly RenderFragment _renderFragment;

    private RenderHandle _renderHandle;
    private bool _hasRendered;

    private TReference _reference = default!;
    private TParameters? _parameters;
    private BitModalExit _exit = default!;
    private int _version;
    private bool _isClosed;



    public BitModalContainerEntry()
    {
        _renderFragment = BuildRenderTree;
    }



    public void Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        var reference = parameters.GetValueOrDefault<TReference>(ReferenceName)!;
        var merged = parameters.GetValueOrDefault<TParameters?>(ParametersName);
        var exit = parameters.GetValueOrDefault<BitModalExit>(ExitName)!;
        var version = parameters.GetValueOrDefault<int>(VersionName);

        if (_hasRendered &&
            ReferenceEquals(reference, _reference) &&
            ReferenceEquals(merged, _parameters) &&
            ReferenceEquals(exit, _exit) &&
            version == _version &&
            reference.IsClosed == _isClosed)
        {
            return Task.CompletedTask;
        }

        _reference = reference;
        _parameters = merged;
        _exit = exit;
        _version = version;
        // The modal reads it when it renders (the modal is open until the service closes it), so a modal that has
        // just closed has to render once more to start its exit animation.
        _isClosed = reference.IsClosed;
        _hasRendered = true;

        _renderHandle.Render(_renderFragment);

        return Task.CompletedTask;
    }



    private void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Not fixed: the content is handed its reference again whenever its modal changed, so that content reading
        // its state stays current.
        builder.OpenComponent<CascadingValue<TReference>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<TReference>.Value), _reference);
        builder.AddComponentParameter(2, nameof(CascadingValue<TReference>.ChildContent), (RenderFragment)(exitBuilder =>
        {
            exitBuilder.OpenComponent<CascadingValue<BitModalExit>>(0);
            exitBuilder.AddComponentParameter(1, nameof(CascadingValue<BitModalExit>.Name), BitModalExit.CascadingName);
            exitBuilder.AddComponentParameter(2, nameof(CascadingValue<BitModalExit>.Value), _exit);
            exitBuilder.AddComponentParameter(3, nameof(CascadingValue<BitModalExit>.IsFixed), true);
            exitBuilder.AddComponentParameter(4, nameof(CascadingValue<BitModalExit>.ChildContent), (RenderFragment)(parametersBuilder =>
            {
                // Not fixed: a refresh hands the modal a new set of merged parameters, which a fixed value would never pass on.
                parametersBuilder.OpenComponent<CascadingValue<TParameters?>>(0);
                parametersBuilder.AddComponentParameter(1, nameof(CascadingValue<TParameters?>.Value), _parameters);
                parametersBuilder.AddComponentParameter(2, nameof(CascadingValue<TParameters?>.ChildContent), _reference.Modal);
                parametersBuilder.CloseComponent();
            }));
            exitBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
