namespace Bit.BlazorUI;

public abstract partial class BitComponentBase : ComponentBase
{
    private BitDir? dir;
    private string? style;
    private string? @class;
    private bool isEnabled = true;
    private BitVisibility visibility;
    private string _uniqueId = BitShortId.NewId();



    protected bool Rendered { get; private set; }

    protected string _Id => Id ?? _uniqueId;



    /// <summary>
    /// The readonly unique id of the root element. it will be assigned to a new Guid at component instance construction.
    /// </summary>
    public string UniqueId => _uniqueId;

    /// <summary>
    /// The ElementReference of the root element.
    /// </summary>
    public ElementReference RootElement { get; internal set; }



    [CascadingParameter] protected BitDir? CascadingDir { get; set; }



    /// <summary>
    /// The aria-label of the control for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the root element of the component.
    /// </summary>
    [Parameter]
    public string? Class
    {
        get => @class;
        set
        {
            if (@class == value) return;

            @class = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Determines the component direction.
    /// </summary>
    [Parameter]
    public BitDir? Dir
    {
        get => dir ?? CascadingDir;
        set
        {
            if (dir == value) return;

            dir = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Capture and render additional attributes in addition to the component's parameters.
    /// </summary>
    [Parameter] public Dictionary<string, object> HtmlAttributes { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Custom id attribute for the root element. if null the UniqueId will be used instead.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// Whether or not the component is enabled.
    /// </summary>
    [Parameter]
    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (isEnabled == value) return;

            isEnabled = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Custom CSS style for the root element of the component.
    /// </summary>
    [Parameter]
    public string? Style
    {
        get => style;
        set
        {
            if (style == value) return;

            style = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether the component is visible, hidden or collapsed.
    /// </summary>
    [Parameter]
    public BitVisibility Visibility
    {
        get => visibility;
        set
        {
            if (visibility == value) return;

            visibility = value;
            OnVisibilityChanged(value);
            StyleBuilder.Reset();
        }
    }


    public override Task SetParametersAsync(ParameterView parameters)
    {
        HtmlAttributes.Clear();
        var parametersDictionary = parameters.ToDictionary() as Dictionary<string, object>;
        foreach (var parameter in parametersDictionary!)
        {
            switch (parameter.Key)
            {
                case nameof(CascadingDir):
                    CascadingDir = (BitDir?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(AriaLabel):
                    AriaLabel = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Class):
                    Class = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Dir):
                    Dir = (BitDir?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Id):
                    Id = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(IsEnabled):
                    IsEnabled = (bool)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Style):
                    Style = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Visibility):
                    Visibility = (BitVisibility)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                default:
                    HtmlAttributes.Add(parameter.Key, parameter.Value);
                    break;
            }
        }
        return base.SetParametersAsync(ParameterView.Empty);
    }

    protected override void OnInitialized()
    {
        RegisterCssStyles();

        StyleBuilder
            .Register(() => style)
            .Register(() => visibility switch
            {
                BitVisibility.Hidden => "visibility:hidden",
                BitVisibility.Collapsed => "display:none",
                _ => string.Empty
            });

        ClassBuilder
              .Register(() => RootElementClass)
              .Register(() => (IsEnabled ? string.Empty : "bit-dis"))
              .Register(() => (Dir == BitDir.Rtl ? "bit-rtl" : string.Empty));

        RegisterCssClasses();

        ClassBuilder.Register(() => @class);

        base.OnInitialized();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        Rendered = true;
        base.OnAfterRender(firstRender);
    }

    protected abstract string RootElementClass { get; }

    protected ElementClassBuilder ClassBuilder { get; private set; } = new ElementClassBuilder();

    protected ElementStyleBuilder StyleBuilder { get; private set; } = new ElementStyleBuilder();

    protected virtual void RegisterCssStyles() { }

    protected virtual void RegisterCssClasses() { }

    protected virtual void OnVisibilityChanged(BitVisibility visibility) { }
}
