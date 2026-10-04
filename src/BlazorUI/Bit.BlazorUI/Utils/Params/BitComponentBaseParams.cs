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
    /// Gets or sets a value indicating whether the component is enabled and can respond to user interaction.
    /// <br />
    /// <see cref="BitComponentBase.IsEnabled"/>.
    /// </summary>
    public bool? IsEnabled { get; set; }

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

        // This runs on every render of every component under the BitParams, so a value that drives the class or
        // the style of the root only resets the builder when it differs from the one the component already holds:
        // an unchanged one would rebuild both strings on every render for nothing.

        if (AriaLabel.HasValue() && bitComponentBase.HasNotBeenSet(nameof(AriaLabel)))
        {
            bitComponentBase.AriaLabel = AriaLabel;
        }

        if (Class.HasValue() && bitComponentBase.HasNotBeenSet(nameof(Class)) && bitComponentBase.Class != Class)
        {
            bitComponentBase.Class = Class;

            bitComponentBase.ClassBuilder.Reset();
        }

        if (Dir.HasValue && bitComponentBase.HasNotBeenSet(nameof(Dir)) && bitComponentBase.Dir != Dir)
        {
            bitComponentBase.Dir = Dir.Value;

            bitComponentBase.ClassBuilder.Reset();
        }

        if (ForceAnimation.HasValue && bitComponentBase.HasNotBeenSet(nameof(ForceAnimation)) && bitComponentBase.ForceAnimation != ForceAnimation)
        {
            bitComponentBase.ForceAnimation = ForceAnimation.Value;

            bitComponentBase.ClassBuilder.Reset();
        }

        if (HtmlAttributes is not null)
        {
            foreach (var attr in HtmlAttributes)
            {
                if (bitComponentBase.HtmlAttributes.ContainsKey(attr.Key)) continue;

                bitComponentBase.HtmlAttributes[attr.Key] = attr.Value;
            }
        }

        if (Id.HasValue() && bitComponentBase.HasNotBeenSet(nameof(Id)))
        {
            bitComponentBase.Id = Id;
        }

        if (IsEnabled.HasValue && bitComponentBase.HasNotBeenSet(nameof(IsEnabled)) && bitComponentBase.IsEnabled != IsEnabled)
        {
            bitComponentBase.IsEnabled = IsEnabled.Value;

            bitComponentBase.ClassBuilder.Reset();
        }

        if (Style.HasValue() && bitComponentBase.HasNotBeenSet(nameof(Style)) && bitComponentBase.Style != Style)
        {
            bitComponentBase.Style = Style;

            bitComponentBase.StyleBuilder.Reset();
        }

        if (TabIndex.HasValue() && bitComponentBase.HasNotBeenSet(nameof(TabIndex)))
        {
            bitComponentBase.TabIndex = TabIndex;
        }

        if (Visibility.HasValue && bitComponentBase.HasNotBeenSet(nameof(Visibility)) && bitComponentBase.Visibility != Visibility)
        {
            bitComponentBase.Visibility = Visibility.Value;

            bitComponentBase.StyleBuilder.Reset();
        }
    }
}
