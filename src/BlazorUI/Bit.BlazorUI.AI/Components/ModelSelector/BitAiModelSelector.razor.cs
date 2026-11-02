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
    /// <remarks>
    /// The options follow the list as it is on each render, a model replaced in place included, and so do those of the
    /// catalog.
    /// </remarks>
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

        BuildItems(source, catalog);

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

    // The options are built on every render, from the models as they are now and the strings as they read now: a model
    // replaced in place of another, or strings that follow the culture, show at once. A parent re-rendering - a chat
    // streaming its answer, say - mostly hands the same models again though, and items built anew would be a change of
    // Items to the dropdown each time, so the dropdown keeps the items it has while they read the same.
    private void BuildItems(IReadOnlyList<BitAiModel> source, IBitAiModelCatalog? catalog)
    {
        _models = source.Where(m => ShowDeprecated || m.IsDeprecated is false)
                        .Where(m => m.Supports(RequiredCapabilities))
                        .ToList();

        var labelled = _models.Select(m => (Model: m, Label: GroupLabel(m, catalog))).ToList();

        // A header is only worth drawing when at least one model has a group to go under; the ones that have none are
        // then gathered under a header of their own rather than left looking like members of the group above them.
        var grouped = labelled.Any(m => m.Label is { Length: > 0 });

        List<BitDropdownItem<string>> items = [];
        foreach (var group in labelled.GroupBy(m => grouped ? m.Label ?? Strings.OtherModels : null, m => m.Model))
        {
            if (group.Key is { Length: > 0 } label)
            {
                items.Add(new() { ItemType = BitDropdownItemType.Header, Text = label });
            }

            foreach (var model in group)
            {
                items.Add(new()
                {
                    Text = model.IsDeprecated ? Strings.ModelDeprecated(model.Name) : model.Name,
                    Value = model.ModelId,
                    Title = model.Description,
                    IconName = model.Icon,
                });
            }
        }

        if (ReadTheSame(items, _items) is false)
        {
            _items = items;
        }
    }

    // Whether two lists of options show the same: every option of one is the same kind, value, text, tooltip and icon as
    // the one of the other at its place.
    private static bool ReadTheSame(List<BitDropdownItem<string>> items, List<BitDropdownItem<string>> others)
    {
        if (items.Count != others.Count) return false;

        for (var i = 0; i < items.Count; i++)
        {
            var (item, other) = (items[i], others[i]);

            if (item.ItemType != other.ItemType ||
                item.Value != other.Value ||
                item.Text != other.Text ||
                item.Title != other.Title ||
                item.IconName != other.IconName) return false;
        }

        return true;
    }

    private static string? GroupLabel(BitAiModel model, IBitAiModelCatalog? catalog)
    {
        return model.Family ?? (model.ProviderId is { } id ? catalog?.FindProvider(id)?.Name ?? id : null);
    }
}
