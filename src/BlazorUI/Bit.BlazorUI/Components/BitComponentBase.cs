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
    /// Captures additional HTML attributes to be applied to the rendered element, in addition to the component's parameters.
    /// <br />
    /// <strong>This parameter should not be assigned directly.</strong>
    /// </summary>
    /// <remarks>
    /// Each entry in the dictionary represents an attribute name and its corresponding value. This
    /// allows customization of the rendered element with other HTML attributes such as alt, title, data-* attributes, and
    /// more.
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
    /// Gets or sets a value indicating whether the component is enabled and can respond to user interaction.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

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

                case nameof(Id):
                    _assignedParameters.Add(nameof(Id));
                    Id = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(IsEnabled):
                    _assignedParameters.Add(nameof(IsEnabled));
                    var isEnabled = (bool)parameter.Value;
                    if (IsEnabled != isEnabled) ClassBuilder.Reset();
                    IsEnabled = isEnabled;
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

        ParametersCache = null;

        return base.SetParametersAsync(ParameterView.Empty);
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

        ClassBuilder
              .Register(() => RootElementClass)
              .Register(() => IsEnabled ? string.Empty : "bit-dis")
              .Register(() => Dir == BitDir.Rtl ? "bit-rtl" : string.Empty);

        RegisterCssClasses();

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
    protected ElementClassBuilder ClassBuilder { get; private set; } = new ElementClassBuilder();

    /// <summary>
    /// Gets the builder used to configure inline CSS styles for the element.
    /// </summary>
    /// <remarks>
    /// Use this property to programmatically construct or modify the element's style attributes before rendering.
    /// Changes made through the builder affect the element's appearance in the rendered output.
    /// </remarks>
    protected ElementStyleBuilder StyleBuilder { get; private set; } = new ElementStyleBuilder();

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
    /// Determines whether the specified parameter has not been assigned a value.
    /// </summary>
    /// <param name="name">
    /// The name of the parameter to check. Cannot be null.
    /// </param>
    /// <returns>
    /// true if the parameter has not been set; otherwise, false.
    /// </returns>
    public bool HasNotBeenSet(string name)
    {
        return _assignedParameters.Contains(name) is false;
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
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
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
