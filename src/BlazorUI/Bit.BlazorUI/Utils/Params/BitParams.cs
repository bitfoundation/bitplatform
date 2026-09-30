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
/// <see cref="Dir"/>, <see cref="IsEnabled"/> and <see cref="ReadOnly"/> reach every component under it whatever its type, below
/// the params object of that type and the component's own parameters.
/// <br />
/// The params objects are read on every render of this component, so assigning a new list or changing a
/// property of one of its objects reaches every component under it once the component that owns them
/// re-renders - and only then, since a render that changes nothing is not passed on to them.
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
    /// The text direction of every bit BlazorUI component under this one, whatever its type. A component that sets
    /// its own Dir, or whose params object does, keeps it. Null leaves it to the ancestors.
    /// </summary>
    [Parameter] public BitDir? Dir { get; set; }

    /// <summary>
    /// Disables every bit BlazorUI component under this one when false, whatever its type, the way a disabled
    /// fieldset disables its form controls. A component that sets its own IsEnabled keeps it, and a nested
    /// <see cref="BitParams"/> can enable a part of the content again with true. Null leaves it to the ancestors.
    /// </summary>
    [Parameter] public bool? IsEnabled { get; set; }

    /// <summary>
    /// Ignores everything the <see cref="BitParams"/> ancestors provide - their params objects,
    /// <see cref="Dir"/>, <see cref="IsEnabled"/> and <see cref="ReadOnly"/> - so the components under this one
    /// take their defaults from this one alone, and their own defaults for everything else.
    /// </summary>
    [Parameter] public bool Isolated { get; set; }

    /// <summary>
    /// List of parameters to provide for the children components.
    /// Of two params objects of the same type, the later one only replaces the parameters it sets.
    /// </summary>
    [Parameter] public IEnumerable<IBitComponentParams>? Parameters { get; set; }

    /// <summary>
    /// Makes every bit BlazorUI input under this one read-only when true, whatever its type. An input that sets
    /// its own ReadOnly keeps it, and a nested <see cref="BitParams"/> can make a part of the content editable
    /// again with false. Null leaves it to the ancestors.
    /// </summary>
    [Parameter] public bool? ReadOnly { get; set; }



    public override Task SetParametersAsync(ParameterView parameters)
    {
        foreach (var parameter in parameters)
        {
            switch (parameter.Name)
            {
                case nameof(ChildContent):
                    ChildContent = (RenderFragment?)parameter.Value;
                    break;

                case nameof(Dir):
                    Dir = (BitDir?)parameter.Value;
                    break;

                case nameof(IsEnabled):
                    IsEnabled = (bool?)parameter.Value;
                    break;

                case nameof(Isolated):
                    Isolated = (bool)parameter.Value;
                    break;

                case nameof(Parameters):
                    Parameters = (IEnumerable<IBitComponentParams>?)parameter.Value;
                    break;

                case nameof(ReadOnly):
                    ReadOnly = (bool?)parameter.Value;
                    break;

                case nameof(ParentScope):
                    ParentScope = (BitParamsScope?)parameter.Value;
                    break;
            }
        }

        var scope = BitParamsScope.Create(ParentScope, Parameters, Isolated, IsEnabled, ReadOnly, Dir);

        // What the params objects carry has not changed, so there is nothing to tell the components under this
        // one: only the content is refreshed, and the cascading values keep the instances they already hold.
        // Rendering them again would make every consumer re-render, since a cascaded object always counts as a
        // possible change.
        if (_contentHost is not null && scope.IsEquivalentTo(_scope))
        {
            _contentHost.Refresh(ChildContent);

            return Task.CompletedTask;
        }

        _scope = scope;

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

        if (_scope is null || _scope.IsPassThrough) return;

        // The scope goes first, so its slot in the render tree stays put while the params objects come and go.
        _values.Add(new(_scope, ScopeName, false, typeof(BitParamsScope)));

        foreach (var (key, value) in _scope.Own)
        {
            _values.Add(new(value, key.Name, false, key.Type));
        }

        // A value of null cascaded under the same type and name hides the ancestor's one from the consumers.
        foreach (var key in _scope.Hidden)
        {
            _values.Add(new(null, key.Name, false, key.Type));
        }

        // The components read the direction the way they read a plain CascadingValue of it, which an isolated
        // scope hides by cascading none.
        if (_scope.Dir is not null || _scope.IsIsolated)
        {
            _values.Add(new(_scope.Dir, null, false, typeof(BitDir?)));
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
