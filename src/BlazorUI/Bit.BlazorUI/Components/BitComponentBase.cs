namespace Bit.BlazorUI;

public abstract partial class BitComponentBase : ComponentBase, IAsyncDisposable
{
    private BitDir? _dir;
    private readonly string _uniqueId = BitShortId.NewId();
    private readonly HashSet<string> _assignedParameters = [];



    protected bool IsRendered;
    protected bool IsDisposed;



    internal string _Id => Id ?? _uniqueId;



    /// <summary>
    /// The readonly unique id of the root element. it will be assigned to a new Guid at component instance construction.
    /// </summary>
    public string UniqueId => _uniqueId;

    /// <summary>
    /// Gets the reference to the root HTML element associated with this component.
    /// </summary>
    /// <remarks>
    /// This property is typically used to perform DOM operations or interop with JavaScript on the
    /// root element of the component.
    /// <br />
    /// The setter is intended for internal use and should not be called directly from
    /// application code.
    /// </remarks>
    public ElementReference RootElement { get; internal set; }


    /// <summary>
    /// Gets or sets the component direction to be cascaded from an ancestor component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from a parent component via Blazor's cascading parameter
    /// mechanism.
    /// <br />
    /// If not set, the component may use a default direction or inherit from a higher-level ancestor.
    /// </remarks>
    [CascadingParameter] protected BitDir? CascadingDir { get; set; }



    /// <summary>
    /// Gets or sets the accessible label for the component, used by assistive technologies.
    /// </summary>
    /// <remarks>
    /// Set this property to provide a descriptive label for screen readers when the component does
    /// not have visible text content.
    /// <br />
    /// This value is rendered as the 'aria-label' attribute in the output markup.
    /// </remarks>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the CSS class name(s) to apply to the rendered element.
    /// </summary>
    /// <remarks>
    /// Multiple class names can be specified by separating them with spaces.
    /// <br />
    /// If the value is null or empty, no additional CSS classes are applied.
    /// </remarks>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the text directionality for the component's content.
    /// </summary>
    /// <remarks>
    /// If not set, the component inherits the directionality from its parent context.
    /// <br />
    /// Use this property to explicitly specify left-to-right or right-to-left text layout when the default inheritance is not desired.
    /// </remarks>
    [Parameter]
    public BitDir? Dir
    {
        get => _dir ?? CascadingDir;
        set => _dir = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the component is disabled and cannot respond to user interaction.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the component's animations play at their full duration
    /// even when reduced motion is requested.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// By default every animation driven by the motion theme variables collapses to a near-zero duration
    /// when the operating system or the browser reports 'prefers-reduced-motion: reduce', which makes the
    /// component appear to change state instantly.
    /// <br />
    /// Setting this to true restores the full durations for this component and its content. Since reduced
    /// motion is an accessibility preference, only opt out of it where the animation carries meaning that
    /// is lost without it.
    /// </remarks>
    [Parameter] public bool ForceAnimation { get; set; }

    /// <summary>
    /// Captures additional HTML attributes to be applied to the rendered element, in addition to the component's parameters.
    /// </summary>
    /// <remarks>
    /// Each entry in the dictionary represents an attribute name and its corresponding value. This
    /// allows customization of the rendered element with other HTML attributes such as alt, title, data-* attributes, and
    /// more.
    /// <br />
    /// Every HTML attribute written on the component lands here. A dictionary passed to this parameter adds its entries
    /// to those, and an attribute written on the component wins over an entry of the same name. The dictionary passed is
    /// copied, never kept or changed.
    /// <br />
    /// This dictionary will be used as the value of the <strong>"@attributes"</strong> blazor directive when rendering the root element of the component.
    /// <br />
    /// If an attribute in the dictionary matches a property already set by the component, the value in the
    /// dictionary may override the default.
    /// </remarks>
    [Parameter] public Dictionary<string, object> HtmlAttributes { get; set; } = [];

    /// <summary>
    /// Gets or sets the unique identifier for the component's root element.
    /// </summary>
    /// <remarks>
    /// Use this property to assign a distinct HTML id attribute to the rendered element. This can be
    /// useful for targeting the element in client-side scripts or for accessibility purposes.
    /// <br />
    /// If the value is null, the <see cref="BitComponentBase.UniqueId"/> will be used as the HTML id attribute of the root element of the component.
    /// </remarks>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the CSS style string to apply to the rendered element.
    /// </summary>
    /// <remarks>
    /// Use this property to specify inline CSS styles for the component. The value should be a valid
    /// CSS style declaration, such as "color: red; font-size: 14px;".
    /// <br />
    /// If not set, no additional inline styles are applied.
    /// </remarks>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// Gets or sets the tab order index for the component when navigating with the keyboard.
    /// </summary>
    /// <remarks>
    /// Set this property to specify the component's position in the tab sequence.
    /// <br />
    /// If not set, the default tab order is determined by the browser.
    /// </remarks>
    [Parameter] public string? TabIndex { get; set; }

    /// <summary>
    /// Gets or sets the visibility state (visible, hidden, or collapsed) of the component.
    /// </summary>
    /// <remarks>
    /// Use this property to control whether the component is displayed or hidden or completely removed from the DOM.
    /// <br />
    /// The value is determined by the <see cref="BitVisibility"/> enumeration, which specifies the available visibility options.
    /// </remarks>
    [Parameter] public BitVisibility Visibility { get; set; }



    protected internal Dictionary<string, object?>? ParametersCache { get; set; }
    public override Task SetParametersAsync(ParameterView parameters)
    {
        _assignedParameters.Clear();
        HtmlAttributes.Clear();
        Dictionary<string, object>? htmlAttributes = null;
        var parametersDictionary = ParametersCache ?? new Dictionary<string, object?>(parameters.ToDictionary());
        foreach (var parameter in parametersDictionary!)
        {
            switch (parameter.Key)
            {
                case nameof(CascadingDir):
                    _assignedParameters.Add(nameof(CascadingDir));
                    var cascadingDir = (BitDir?)parameter.Value;
                    if (CascadingDir != cascadingDir) ClassBuilder.Reset();
                    CascadingDir = cascadingDir;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(AriaLabel):
                    _assignedParameters.Add(nameof(AriaLabel));
                    AriaLabel = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Class):
                    _assignedParameters.Add(nameof(Class));
                    var @class = (string?)parameter.Value;
                    if (Class != @class) ClassBuilder.Reset();
                    Class = @class;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Dir):
                    _assignedParameters.Add(nameof(Dir));
                    var dir = (BitDir?)parameter.Value;
                    if (Dir != dir) ClassBuilder.Reset();
                    Dir = dir;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Disabled):
                    _assignedParameters.Add(nameof(Disabled));
                    var disabled = (bool)parameter.Value;
                    if (Disabled != disabled) ClassBuilder.Reset();
                    Disabled = disabled;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(ForceAnimation):
                    _assignedParameters.Add(nameof(ForceAnimation));
                    var forceAnimation = (bool)parameter.Value;
                    if (ForceAnimation != forceAnimation) ClassBuilder.Reset();
                    ForceAnimation = forceAnimation;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                // Merged into the splatted attributes once every other one is in, never taken as the dictionary
                // itself: HtmlAttributes is cleared on every pass, which would empty the caller's own dictionary.
                case nameof(HtmlAttributes):
                    _assignedParameters.Add(nameof(HtmlAttributes));
                    htmlAttributes = (Dictionary<string, object>?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Id):
                    _assignedParameters.Add(nameof(Id));
                    Id = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Style):
                    _assignedParameters.Add(nameof(Style));
                    var style = (string?)parameter.Value;
                    if (Style != style) StyleBuilder.Reset();
                    Style = style;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(TabIndex):
                    _assignedParameters.Add(nameof(TabIndex));
                    var tabindex = (string?)parameter.Value;
                    TabIndex = tabindex;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Visibility):
                    _assignedParameters.Add(nameof(Visibility));
                    var visibility = (BitVisibility)parameter.Value;
                    if (Visibility != visibility) StyleBuilder.Reset();
                    Visibility = visibility;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                default:
                    HtmlAttributes.Add(parameter.Key, parameter.Value);
                    break;
            }
        }

        MergeHtmlAttributesParameter(htmlAttributes);

        ParametersCache = null;

        var restore = RestoreDroppedCascadeParameters();

        return restore.IsCompletedSuccessfully ? base.SetParametersAsync(ParameterView.Empty) : SetParametersAfterAsync(restore);
    }

    private async Task SetParametersAfterAsync(Task restore)
    {
        await restore;

        await base.SetParametersAsync(ParameterView.Empty);
    }

    /// <summary>
    /// Adds the entries of a dictionary passed as the HtmlAttributes parameter to the attributes the page wrote on the
    /// component one by one, which win over an entry of the same name however either of them cases it - the render tree
    /// treats differently cased names as one attribute, so both would otherwise reach it and the last one would win.
    /// </summary>
    private void MergeHtmlAttributesParameter(Dictionary<string, object>? htmlAttributes)
    {
        if (htmlAttributes is null || htmlAttributes.Count == 0) return;

        HashSet<string>? written = HtmlAttributes.Count == 0 ? null : new(HtmlAttributes.Keys, StringComparer.OrdinalIgnoreCase);

        foreach (var attribute in htmlAttributes)
        {
            if (written?.Contains(attribute.Key) is true) continue;

            HtmlAttributes[attribute.Key] = attribute.Value;
        }
    }



    protected override void OnInitialized()
    {
        RegisterCssStyles();

        StyleBuilder
            .Register(() => Style)
            .Register(() => Visibility switch
            {
                BitVisibility.Hidden => "visibility:hidden",
                BitVisibility.Collapsed => "display:none",
                _ => string.Empty
            });

        RegisterCssClasses();

        ClassBuilder
              .Register(() => RootElementClass)
              .Register(() => Disabled ? "bit-dis" : string.Empty)
              .Register(() => Dir == BitDir.Rtl ? "bit-rtl" : string.Empty)
              .Register(() => ForceAnimation ? "bit-fam" : string.Empty);

        ClassBuilder.Register(() => Class);

        base.OnInitialized();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        IsRendered = true;
        base.OnAfterRender(firstRender);
    }



    /// <summary>
    /// Gets the CSS class name to apply to the root element of the component.
    /// </summary>
    /// <remarks>
    /// Derived classes should override this property to specify the appropriate CSS class for the
    /// root element.
    /// <br />
    /// This value is typically used to control the styling of the component's outermost HTML element.
    /// </remarks>
    protected abstract string RootElementClass { get; }

    /// <summary>
    /// Gets the builder used to construct CSS class attribute values for the element.
    /// </summary>
    /// <remarks>
    /// This property is intended for use by derived classes to manage and compose CSS classes dynamically. 
    /// It is not accessible outside the class hierarchy.
    /// </remarks>
    public ElementClassBuilder ClassBuilder { get; private set; } = new();

    /// <summary>
    /// Gets the builder used to configure inline CSS styles for the element.
    /// </summary>
    /// <remarks>
    /// Use this property to programmatically construct or modify the element's style attributes before rendering.
    /// Changes made through the builder affect the element's appearance in the rendered output.
    /// </remarks>
    public ElementStyleBuilder StyleBuilder { get; private set; } = new();

    /// <summary>
    /// Registers the CSS styles required for the component.
    /// </summary>
    /// <remarks>
    /// Override this method in a derived class to add custom CSS styles during the component's setup or rendering process.
    /// This method is typically called as part of the component's initialization sequence.
    /// </remarks>
    protected virtual void RegisterCssStyles() { }

    /// <summary>
    /// Registers CSS classes for the control. Called during the component's initialization to allow derived classes to
    /// add or modify CSS class assignments.
    /// </summary>
    /// <remarks>
    /// Override this method in a derived class to customize the set of CSS classes applied to the component.
    /// This method is typically invoked as part of the control's setup process and should not be called directly.
    /// </remarks>
    protected virtual void RegisterCssClasses() { }

    /// <summary>
    /// Called when the visibility state changes.
    /// </summary>
    /// <remarks>
    /// Override this method in a derived class to respond to changes in visibility.
    /// This method is invoked whenever the visibility state is updated.
    /// </remarks>
    /// <param name="visibility">
    /// The new visibility state that triggered the change event.
    /// </param>
    protected virtual void OnVisibilityChanged(BitVisibility visibility) { }

    /// <summary>
    /// Splices two style parts into the single declaration list a style attribute holds.
    /// </summary>
    /// <remarks>
    /// Two parts landing in the same style attribute are only two declarations while a semicolon stands
    /// between them: a part that was written without a trailing one would otherwise swallow the declaration
    /// that follows it, and the CSS parser drops both.
    /// </remarks>
    private protected static string? JoinStyles(string? style, string? extraStyle)
    {
        if (style.HasNoValue()) return extraStyle;

        if (extraStyle.HasNoValue()) return style;

        return style!.TrimEnd().EndsWith(';') ? $"{style} {extraStyle}" : $"{style};{extraStyle}";
    }

    /// <summary>
    /// The style the root has without the style of a passing state of it (Styles.Focused, Styles.Toggled) while
    /// that state is applied, and null otherwise: the steady part of the root's style, followed by Style.
    /// </summary>
    /// <remarks>
    /// A component with a popup renders it as the data-bit-popup-style of its root, which Callouts.ts copies onto
    /// the chain the popup is relocated into in place of the style attribute, so the popup never takes on a look
    /// that comes and goes as the focus moves between the field and the popup itself. An empty string, rather than
    /// null, is what says the root has no steady style to hand the popup at all.
    /// </remarks>
    private protected string? GetPopupStyle(bool inPassingState, string? passingStyle, string? steadyStyle)
    {
        if (inPassingState is false || passingStyle.HasNoValue()) return null;

        return JoinStyles(steadyStyle, Style) ?? string.Empty;
    }

    /// <summary>
    /// Splices two class lists into the single list a class attribute holds.
    /// </summary>
    /// <remarks>
    /// Two lists landing in the same class attribute are only two lists while a space stands between them.
    /// </remarks>
    private protected static string? JoinClasses(string? @class, string? extraClass)
    {
        if (@class.HasNoValue()) return extraClass;

        if (extraClass.HasNoValue()) return @class;

        return $"{@class} {extraClass}";
    }

    /// <summary>
    /// Splices two space-separated lists of tokens - the ids of an aria-owns, the keys of an aria-keyshortcuts - into
    /// the single list the attribute holds, and null where neither has anything in it.
    /// </summary>
    private protected static string? JoinTokenLists(string? list, string? extraList)
    {
        var joined = JoinClasses(list, extraList);

        return joined.HasValue() ? joined : null;
    }

    /// <summary>
    /// The value of an attribute the page wrote as a plain HTML attribute rather than as a parameter of the component.
    /// </summary>
    /// <remarks>
    /// HTML attribute names are case insensitive, and so is the deduplication the render tree does between a splatted
    /// attribute and one the component writes itself, so a differently cased spelling has to be found here too. This is
    /// what lets a component resolve its own value against the splatted one instead of writing a null over it - a null
    /// written over a splatted attribute does not leave that attribute alone, it removes it.
    /// </remarks>
    private protected string? GetSplattedAttribute(string name)
        => TryGetSplattedAttribute(name, out var value) ? value : null;

    /// <summary>
    /// Determines whether the page wrote an attribute as a plain HTML attribute, however it cased its name and whatever
    /// value it gave it - a null or a false included, which is how a page takes an attribute off the element.
    /// </summary>
    private protected bool HasSplattedAttribute(string name) => TryGetSplattedAttribute(name, out _);

    /// <summary>
    /// Looks up an attribute the page wrote as a plain HTML attribute, matching its name case insensitively the way the
    /// render tree does. True whenever the attribute was written, even with a null or a false value, which the value
    /// comes out as null for.
    /// </summary>
    private protected bool TryGetSplattedAttribute(string name, out string? value)
    {
        value = null;

        if (HtmlAttributes.Count == 0) return false;

        // The render tree keeps the last of several differently cased spellings, so the last one in render order wins
        // here too, an exact match included.
        var found = false;
        object? selected = null;

        foreach (var attribute in HtmlAttributes)
        {
            if (string.Equals(attribute.Key, name, StringComparison.OrdinalIgnoreCase) is false) continue;

            selected = attribute.Value;
            found = true;
        }

        if (found is false) return false;

        value = StringifyAttributeValue(selected);
        return true;
    }

    /// <summary>
    /// The aria-label the root is named with: the AriaLabel parameter, or else the aria-label the page splatted on.
    /// </summary>
    /// <remarks>
    /// The root's aria-label is written after the splatted attributes, and a null written over a splatted attribute
    /// removes it, so the parameter is resolved against what the page wrote by hand rather than written over it.
    /// </remarks>
    private protected string? ResolveAriaLabel() => AriaLabel ?? GetSplattedAttribute("aria-label");

    // A boolean is the one attribute value the renderer does not write as its text: an attribute is written with no
    // value at all while it is true and left out altogether while it is false, which is what an attribute splatted
    // onto a plain element does. So the two are resolved into the same thing here rather than into the "True" and
    // "False" that ToString would give an attribute the component writes itself.
    private static string? StringifyAttributeValue(object? value)
    {
        if (value is bool boolean) return boolean ? string.Empty : null;

        return value?.ToString();
    }

    /// <summary>
    /// Determines whether a tag name is one the renderer builds an element of.
    /// </summary>
    /// <remarks>
    /// A tag name is only a tag name while both the markup it is written into reads it as one and the browser builds
    /// an element of it. The HTML parser only begins a tag at all when a letter follows the "&lt;", and it ends the name
    /// at the first whitespace and the tag at the first "&gt;", so a name carrying either of them would write markup of
    /// its own rather than name an element. What the browser accepts is the narrower of the two and the engines do not
    /// agree on it: the DOM standard now takes any name that begins with a letter and carries no whitespace, "/" or
    /// "&gt;", while the rule it replaced - which WebKit still enforces - is the XML name, and a name refused by
    /// document.createElement throws where the renderer builds the element, taking the whole render batch with it.
    /// So the name is read as what a name is made of rather than as what it must not contain: the ASCII letters and
    /// digits, the four characters that join them in every markup language that has tag names, and the letters and
    /// digits of the other alphabets, which is all a custom element may be named in.
    /// </remarks>
    private protected static bool IsValidElement(string element)
    {
        if (element.Length == 0) return false;

        if (char.IsAsciiLetter(element[0]) is false) return false;

        foreach (var @char in element)
        {
            if (char.IsAsciiLetterOrDigit(@char)) continue;

            if (@char is '-' or '_' or '.' or ':') continue;

            // Everything outside ASCII that is a letter or a digit is a name of some alphabet; the rest of it - the
            // separators, the punctuation, the C1 controls - is refused along with the ASCII symbols and whitespace.
            if (char.IsAscii(@char) is false && char.IsLetterOrDigit(@char)) continue;

            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether a tag name is one of the HTML void elements, which are defined to hold no content.
    /// </summary>
    /// <remarks>
    /// A void element is defined to have no content at all, so a closing tag and any child content are invalid markup
    /// in it. The static HTML renderer writes it self-closed and silently drops whatever follows, so a component that
    /// renders a tag the page names asks this before it writes any content into it.
    /// </remarks>
    private protected static bool IsVoidElement(string element)
    {
        return _VoidElements.Contains(element);
    }

    /// <summary>
    /// Resolves the tag a page names for the root of a component that writes content into it.
    /// </summary>
    /// <remarks>
    /// The name is used as written, less its surrounding whitespace, while it is a name a tag can have and one that can
    /// hold content; anything else - a void element included - falls back to the given default.
    /// </remarks>
    private protected static string ResolveContentElement(string? element, string fallback)
    {
        element = element?.Trim();

        return element.HasValue() && IsValidElement(element!) && IsVoidElement(element!) is false ? element! : fallback;
    }

    // The obsolete four (basefont, bgsound, frame and keygen) are in the list the HTML parser itself treats as void,
    // so a browser drops their content just the same and they belong here with the rest.
    private static readonly HashSet<string> _VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "basefont", "bgsound", "br", "col", "embed", "frame", "hr",
        "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr"
    };



    /// <summary>
    /// Determines whether the specified parameter has not been assigned a value by the markup on this render.
    /// </summary>
    /// <remarks>
    /// The answer covers every parameter of the component, whichever class in its hierarchy declares it: the shared
    /// parameters of <see cref="BitComponentBase"/>, those of an intermediate base such as
    /// <see cref="BitInputBase{TValue}"/>, and the ones the component declares itself. So it is the same whatever
    /// the static type of the reference it is called through.
    /// </remarks>
    /// <param name="name">
    /// The name of the parameter to check. Cannot be null.
    /// </param>
    /// <returns>
    /// true if the parameter has not been set; otherwise, false.
    /// </returns>
    public virtual bool HasNotBeenSet(string name)
    {
        return IsSetByMarkup(name) is false;
    }



    /// <summary>
    /// Asynchronously releases the unmanaged resources used by the object and optionally releases the managed resources.
    /// </summary>
    /// <remarks>
    /// Call this method when you are finished using the object to ensure that all resources are released promptly.
    /// After calling DisposeAsync, the object should not be used further.
    /// </remarks>
    /// <returns>
    /// A ValueTask that represents the asynchronous dispose operation.
    /// </returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await DisposeAsync(true);
        }
        // A circuit on its way down cancels the interop calls that are still in flight rather than refusing them
        // with a JSDisconnectedException, so a component unregistering its listeners is answered with a
        // cancellation - which the renderer then logs as an unhandled disposal error. Every DisposeAsync
        // override would otherwise have to catch it beside the JSDisconnectedException it already catches, and
        // there is nothing a disposal could do about it in any case: the component is going, and so is the page.
        catch (OperationCanceledException) { }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the object and optionally releases the managed resources asynchronously.
    /// </summary>
    /// <remarks>
    /// Override this method to provide custom asynchronous resource cleanup logic.
    /// This method is called by DisposeAsync().
    /// </remarks>
    /// <param name="disposing">
    /// true to release both managed and unmanaged resources; false to release only unmanaged resources.
    /// </param>
    /// <returns>
    /// A ValueTask that represents the asynchronous dispose operation.
    /// </returns>
    protected virtual ValueTask DisposeAsync(bool disposing)
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}
