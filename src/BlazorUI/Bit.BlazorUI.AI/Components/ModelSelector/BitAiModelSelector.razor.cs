using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiModelSelector is a model picker driven by the registered model catalog, its models grouped by family or
/// provider.
/// </summary>
/// <remarks>
/// The picker is a <see cref="BitDropdown{TItem, TValue}"/>. It renders nothing while no model is left to offer, and it
/// never shows a model the bound <see cref="ModelId"/> does not name without saying so: a selection it had to make on
/// its own is raised through <see cref="ModelIdChanged"/> and <see cref="OnModelSelected"/>.
/// </remarks>
public partial class BitAiModelSelector : BitAiComponentBase
{
    private string? _announcedId;
    private string? _receivedId;
    private BitAiModel? _selected;
    private List<BitAiModel> _models = [];
    private List<BitDropdownItem<string>> _items = [];



    [Inject] private IServiceProvider _services { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the model selector component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple model selector components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiModelSelectorParams.ParamName)]
    public BitAiModelSelectorParams? CascadingParameters { get; set; }



    /// <summary>
    /// The catalog of the models to offer. When null, the <see cref="IBitAiModelCatalog"/> registered in the services is
    /// used.
    /// </summary>
    [Parameter] public IBitAiModelCatalog? Catalog { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the model selector.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiModelSelectorClassStyles? Classes { get; set; }

    /// <summary>
    /// The id of the selected model.
    /// </summary>
    /// <remarks>
    /// When this names no model in the list - because it is unset, deprecated, or filtered out - the picker selects the
    /// default model of the catalog, or else the first one, and raises <see cref="ModelIdChanged"/> and
    /// <see cref="OnModelSelected"/> for it, so a bound value never disagrees with what the picker shows.
    /// </remarks>
    [Parameter] public string? ModelId { get; set; }

    /// <summary>
    /// Raised when the selection changes, so the value can be bound.
    /// </summary>
    [Parameter] public EventCallback<string> ModelIdChanged { get; set; }

    /// <summary>
    /// The models to offer, in place of those of the catalog, for a host that filters them by entitlement.
    /// </summary>
    [Parameter] public IReadOnlyList<BitAiModel>? Models { get; set; }

    /// <summary>
    /// Raised with the full model record when the selection changes.
    /// </summary>
    [Parameter] public EventCallback<BitAiModel> OnModelSelected { get; set; }

    /// <summary>
    /// Offers only the models that declare every one of these capabilities, for example
    /// <see cref="BitAiModelCapabilities.Vision"/> for a composer that has images staged.
    /// <br />
    /// The default value is <strong>None</strong>.
    /// </summary>
    [Parameter] public BitAiModelCapabilities RequiredCapabilities { get; set; } = BitAiModelCapabilities.None;

    /// <summary>
    /// Offers the models marked deprecated too, labelled as such.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool ShowDeprecated { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the model selector.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiModelSelectorClassStyles? Styles { get; set; }



    /// <summary>
    /// The model the picker shows, which may differ from <see cref="ModelId"/> until the change is raised.
    /// </summary>
    public BitAiModel? Selected => _selected;



    protected override string RootElementClass => "bit-aimds";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiModelSelectorParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // A bound value the parent has moved since - back to null, say, after the picker had filled it in - is announced
        // again when the picker has to fall back once more, or the picker would show a model the bound value does not
        // name. One that stays where it was is not, so a parent that never takes the value up is not sent the same
        // fallback after every render of its own.
        if (string.Equals(_receivedId, ModelId, StringComparison.Ordinal) is false)
        {
            _receivedId = ModelId;
            _announcedId = null;
        }

        var catalog = Catalog ?? _services.GetService(typeof(IBitAiModelCatalog)) as IBitAiModelCatalog;
        var source = Models ?? catalog?.Models ?? [];

        _models = source.Where(m => ShowDeprecated || m.IsDeprecated is false)
                        .Where(m => m.Supports(RequiredCapabilities))
                        .ToList();

        // A header is only worth drawing when at least one model has a group to go under; the ones that have none are
        // then gathered under a header of their own rather than left looking like members of the group above them.
        var grouped = _models.Any(m => GroupLabel(m, catalog) is { Length: > 0 });

        _items = [];
        foreach (var group in _models.GroupBy(m => grouped ? GroupLabel(m, catalog) ?? Strings.OtherModels : null))
        {
            if (group.Key is { Length: > 0 } label)
            {
                _items.Add(new() { ItemType = BitDropdownItemType.Header, Text = label });
            }

            foreach (var model in group)
            {
                _items.Add(new()
                {
                    Text = model.IsDeprecated ? Strings.ModelDeprecated(model.Name) : model.Name,
                    Value = model.ModelId,
                    Title = model.Description,
                    IconName = model.Icon,
                });
            }
        }

        _selected = _models.FirstOrDefault(m => m.ModelId == ModelId)
                    ?? _models.FirstOrDefault(m => m.ModelId == catalog?.DefaultModel?.ModelId)
                    ?? _models.FirstOrDefault();

        base.OnParametersSet();
    }

    /// <remarks>
    /// The picker shows the model it fell back to when the bound value names none, but nothing would tell the
    /// application so. Raising the change here, after the render, keeps the bound value and the picker in step without
    /// re-entering the render of the parent.
    /// </remarks>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_selected is not { } selected || selected.ModelId == ModelId) return;
        if (_announcedId == selected.ModelId) return;

        _announcedId = selected.ModelId;
        ModelId = selected.ModelId;

        await ModelIdChanged.InvokeAsync(selected.ModelId);
        await OnModelSelected.InvokeAsync(selected);
    }



    private async Task HandleOnValueChanged(string? id)
    {
        if (Disabled || id is null) return;

        ModelId = id;
        _announcedId = id;
        _selected = _models.FirstOrDefault(m => m.ModelId == id);

        await ModelIdChanged.InvokeAsync(id);

        if (_selected is not null)
        {
            await OnModelSelected.InvokeAsync(_selected);
        }
    }

    private static string? GroupLabel(BitAiModel model, IBitAiModelCatalog? catalog)
    {
        return model.Family ?? (model.ProviderId is { } id ? catalog?.FindProvider(id)?.Name ?? id : null);
    }
}
