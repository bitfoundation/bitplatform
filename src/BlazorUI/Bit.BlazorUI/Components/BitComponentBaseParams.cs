namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitComponentBase"/> component.
/// </summary>
public abstract class BitComponentBaseParams
{
    /// <summary>
    /// Gets or sets the accessible label for the component, used by assistive technologies.
    /// Every component that reads this params object is announced by the same name, so only share it between
    /// components that do the same thing.
    /// <br />
    /// <see cref="BitComponentBase.AriaLabel"/>.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the CSS class name(s) to apply to the rendered element.
    /// <br />
    /// <see cref="BitComponentBase.Class"/>.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the text directionality for the component's content.
    /// <br />
    /// <see cref="BitComponentBase.Dir"/>.
    /// </summary>
    public BitDir? Dir { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the component is disabled and cannot respond to user interaction.
    /// <br />
    /// <see cref="BitComponentBase.Disabled"/>.
    /// </summary>
    public bool? Disabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the component's animations play at their full duration
    /// even when reduced motion is requested.
    /// <br />
    /// <see cref="BitComponentBase.ForceAnimation"/>.
    /// </summary>
    public bool? ForceAnimation { get; set; }

    /// <summary>
    /// Captures additional HTML attributes to be applied to the rendered element, in addition to the component's parameters.
    /// A nested params object of the same type adds its entries to these instead of replacing them.
    /// <br />
    /// <see cref="BitComponentBase.HtmlAttributes"/>.
    /// </summary>
    public Dictionary<string, object>? HtmlAttributes { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the component's root element.
    /// Every component that reads this params object gets the same id, so only share it with a single component.
    /// <br />
    /// <see cref="BitComponentBase.Id"/>.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the CSS style string to apply to the rendered element.
    /// <br />
    /// <see cref="BitComponentBase.Style"/>.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Gets or sets the tab order index for the component when navigating with the keyboard.
    /// <br />
    /// <see cref="BitComponentBase.TabIndex"/>.
    /// </summary>
    public string? TabIndex { get; set; }

    /// <summary>
    /// Gets or sets the visibility state (visible, hidden, or collapsed) of the component.
    /// <br />
    /// <see cref="BitComponentBase.Visibility"/>.
    /// </summary>
    public BitVisibility? Visibility { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitComponentBase"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitComponentBase"/> itself.
    /// </summary>
    /// <param name="bitComponentBase">
    /// The <see cref="BitComponentBase"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateBaseParameters(BitComponentBase bitComponentBase)
    {
        if (bitComponentBase is null) return;

        if (AriaLabel.HasValue())
        {
            bitComponentBase.TakeFromCascade(nameof(AriaLabel), AriaLabel, static c => c.AriaLabel, static (c, v) => c.AriaLabel = v);
        }

        if (Class.HasValue())
        {
            bitComponentBase.TakeFromCascade(nameof(Class), Class, static c => c.Class, static (c, v) => c.Class = v);
        }

        if (Dir.HasValue)
        {
            bitComponentBase.TakeFromCascade(nameof(Dir), Dir.Value, static c => c.Dir, static (c, v) => c.Dir = v);
        }

        if (Disabled.HasValue)
        {
            bitComponentBase.TakeFromCascade(nameof(Disabled), Disabled.Value, static c => c.Disabled, static (c, v) => c.Disabled = v);
        }

        if (ForceAnimation.HasValue)
        {
            bitComponentBase.TakeFromCascade(nameof(ForceAnimation), ForceAnimation.Value, static c => c.ForceAnimation, static (c, v) => c.ForceAnimation = v);
        }

        if (HtmlAttributes is not null)
        {
            foreach (var attr in HtmlAttributes)
            {
                if (bitComponentBase.HtmlAttributes.ContainsKey(attr.Key)) continue;

                bitComponentBase.HtmlAttributes[attr.Key] = attr.Value;
            }
        }

        if (Id.HasValue())
        {
            bitComponentBase.TakeFromCascade(nameof(Id), Id, static c => c.Id, static (c, v) => c.Id = v);
        }

        if (Style.HasValue())
        {
            bitComponentBase.TakeFromCascade(nameof(Style), Style, static c => c.Style, static (c, v) => c.Style = v);
        }

        if (TabIndex.HasValue())
        {
            bitComponentBase.TakeFromCascade(nameof(TabIndex), TabIndex, static c => c.TabIndex, static (c, v) => c.TabIndex = v);
        }

        if (Visibility.HasValue)
        {
            bitComponentBase.TakeFromCascade(nameof(Visibility), Visibility.Value, static c => c.Visibility, static (c, v) => c.Visibility = v);
        }
    }
}
