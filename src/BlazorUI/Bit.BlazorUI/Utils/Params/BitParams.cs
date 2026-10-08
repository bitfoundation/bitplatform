using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Provides cascading parameter values for the bit BlazorUI components.
/// </summary>
/// <remarks>
/// Every component under it takes each parameter it did not set for itself from the params object of its
/// type, so what a <see cref="BitParams"/> carries is a default and never an override.
/// <br />
/// A nested <see cref="BitParams"/> refines the one around it: a params object of a type the outer one carries
/// too only replaces the parameters it sets, and the rest are still taken from the outer one, unless
/// <see cref="Isolated"/> is set.
/// <br />
/// The params objects are read on every render of this component, so assigning a new list or changing a
/// property of one of its objects reaches every component under it once the component that owns them
/// re-renders - and only then, since a render that changes nothing is not passed on to them. A parameter a params
/// object stops setting - cleared, removed with its object, or hidden by an <see cref="Isolated"/> one - goes back
/// to the value the component held before.
/// </remarks>
public class BitParams : ComponentBase
{
    /// <summary>
    /// The name the scope of the nearest <see cref="BitParams"/> ancestor is cascaded with, which is what lets a
    /// nested one take over what its ancestors carry.
    /// </summary>
    internal const string ScopeName = $"{nameof(BitParams)}.{nameof(BitParamsScope)}";



    private BitParamsScope? _scope;
    private ContentHost? _contentHost;
    private readonly RenderFragment _renderContent;
    private readonly List<BitCascadingValue> _values = [];
    private readonly List<BitParamsKey> _cascadedKeys = [];



    public BitParams()
    {
        _renderContent = RenderContent;
    }



    [CascadingParameter(Name = ScopeName)] private BitParamsScope? ParentScope { get; set; }



    /// <summary>
    /// The content to which the values should be provided.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Ignores the params objects the <see cref="BitParams"/> ancestors provide, so the components under this one
    /// take their defaults from this one alone, and their own defaults for everything else.
    /// </summary>
    [Parameter] public bool Isolated { get; set; }

    /// <summary>
    /// List of parameters to provide for the children components.
    /// Of two params objects of the same type, the later one only replaces the parameters it sets.
    /// </summary>
    [Parameter] public IEnumerable<IBitComponentParams>? Parameters { get; set; }



    public override Task SetParametersAsync(ParameterView parameters)
    {
        foreach (var parameter in parameters)
        {
            switch (parameter.Name)
            {
                case nameof(ChildContent):
                    ChildContent = (RenderFragment?)parameter.Value;
                    break;

                case nameof(Isolated):
                    Isolated = (bool)parameter.Value;
                    break;

                case nameof(Parameters):
                    Parameters = (IEnumerable<IBitComponentParams>?)parameter.Value;
                    break;

                case nameof(ParentScope):
                    ParentScope = (BitParamsScope?)parameter.Value;
                    break;
            }
        }

        // What the params objects carry has not changed, so there is nothing to tell the components under this
        // one: only the content is refreshed, and the cascading values keep the instances they already hold.
        // Rendering them again would make every consumer re-render, since a cascaded object always counts as a
        // possible change.
        if (_contentHost is not null && _scope is not null && _scope.IsCreatedFrom(ParentScope, Parameters, Isolated))
        {
            _contentHost.Refresh(ChildContent);

            return Task.CompletedTask;
        }

        _scope = BitParamsScope.Create(ParentScope, Parameters, Isolated, _scope);

        UpdateValues();

        return base.SetParametersAsync(ParameterView.Empty);
    }



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IBitComponentParams))]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
#pragma warning disable ASP0006, IL2110
        builder.OpenComponent<BitCascadingValueProvider>(0);
        builder.AddComponentParameter(1, nameof(BitCascadingValueProvider.Values), _values);
        builder.AddComponentParameter(2, nameof(BitCascadingValueProvider.ChildContent), _renderContent);
        builder.CloseComponent();
#pragma warning restore ASP0006, IL2110
    }



    private void UpdateValues()
    {
        _values.Clear();

        if (_scope is null) return;

        // The ancestors' params objects are cascaded again here as well, whether this one is isolated or not, so
        // that becoming isolated, or no longer, changes what the chain carries and never its shape.
        foreach (var (key, _) in _scope.Own)
        {
            if (_cascadedKeys.Contains(key) is false) _cascadedKeys.Add(key);
        }

        foreach (var key in ParentScope?.All.Keys ?? [])
        {
            if (_cascadedKeys.Contains(key) is false) _cascadedKeys.Add(key);
        }

        // The scope goes first, and is cascaded even when this one adds nothing to it, so that the chain never
        // goes from no value at all to some.
        _values.Add(new(_scope, ScopeName, false, typeof(BitParamsScope)));

        // Every value keeps the slot it was first rendered in, and none is ever dropped: the provider renders its
        // content at the end of the chain, so a chain that changes its length or its order tears down and builds
        // again every component under it, with all the state they hold. A key this one stops supplying is cascaded
        // with whatever is in effect for it instead - the ancestors' object, or a null that hides theirs from the
        // content of an isolated one.
        foreach (var key in _cascadedKeys)
        {
            _values.Add(new(_scope.All.GetValueOrDefault(key), key.Name, false, key.Type));
        }
    }

    private void RenderContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<ContentHost>(0);
        builder.AddComponentParameter(1, nameof(ContentHost.ChildContent), ChildContent);
        builder.AddComponentReferenceCapture(2, host => _contentHost = (ContentHost)host);
        builder.CloseComponent();
    }



    /// <summary>
    /// Renders the content of a <see cref="BitParams"/>, so it can be refreshed on its own whenever the params
    /// objects stay the same and the cascading values around it need not be rendered again.
    /// </summary>
    private sealed class ContentHost : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }

        public void Refresh(RenderFragment? childContent)
        {
            ChildContent = childContent;

            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, ChildContent);
        }
    }
}
