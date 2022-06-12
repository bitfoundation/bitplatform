using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI;

public abstract partial class BitComponentBase : ComponentBase
{
    private Visual visual;
    private string? style;
    private string? @class;
    private bool isEnabled = true;
    private BitComponentVisibility visibility;

    protected bool Rendered { get; private set; }

    private Guid _uniqueId = Guid.NewGuid();

    public Guid UniqueId => _uniqueId;

    public ElementReference RootElement { get; internal set; }

    [CascadingParameter] public Theme Theme { get; set; }

    [CascadingParameter]
    public Visual Visual
    {
        get => visual;
        set
        {
            visual = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Custom style for the root element of the component
    /// </summary>
    [Parameter]
    public string? Style
    {
        get => style;
        set
        {
            style = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Custom CSS class for the root element of the component
    /// </summary>
    [Parameter]
    public string? Class
    {
        get => @class;
        set
        {
            @class = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether or not the component is enabled
    /// </summary>
    [Parameter]
    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            isEnabled = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether the component is visible, hidden, collapsed
    /// </summary>
    [Parameter]
    public BitComponentVisibility Visibility
    {
        get => visibility;
        set
        {
            if (visibility == value) return;
            visibility = value;
            OnComponentVisibilityChanged(value);
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// The aria-label of the control for the benefit of screen readers
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// Capture and render additional attributes in addition to the component's parameters
    /// </summary>
    [Parameter]
#pragma warning disable CA2227 // Collection properties should be read only
    public Dictionary<string, object> HtmlAttributes { get; set; } = new Dictionary<string, object>();
#pragma warning restore CA2227 // Collection properties should be read only

    public override Task SetParametersAsync(ParameterView parameters)
    {
        HtmlAttributes.Clear();
        var parametersDictionary = parameters.ToDictionary() as Dictionary<string, object>;
        foreach (var parameter in parametersDictionary!)
        {
            switch (parameter.Key)
            {
                case nameof(Theme):
                    Theme = (Theme)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Visual):
                    Visual = (Visual)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Style):
                    Style = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Class):
                    Class = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(IsEnabled):
                    IsEnabled = (bool)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Visibility):
                    Visibility = (BitComponentVisibility)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(AriaLabel):
                    AriaLabel = (string?)parameter.Value;
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
        RegisterComponentStyles();
        StyleBuilder
            .Register(() => Style)
            .Register(() => Visibility == BitComponentVisibility.Hidden ? "visibility:hidden" :
                            Visibility == BitComponentVisibility.Collapsed ? "display:none" :
                            string.Empty);

        ClassBuilder
            .Register(() => RootElementClass)
            .Register(() => $"{RootElementClass}-{VisualClassRegistrar()}")
            .Register(() => $"{RootElementClass}-{(IsEnabled ? "enabled" : "disabled")}-{VisualClassRegistrar()}");
        RegisterComponentClasses();
        ClassBuilder.Register(() => Class);

        base.OnInitialized();
    }

    protected virtual string VisualClassRegistrar()
    {
        return Visual == Visual.Cupertino ? "cupertino" : Visual == Visual.Material ? "material" : "fluent";
    }

    protected override void OnAfterRender(bool firstRender)
    {
        Rendered = true;
        base.OnAfterRender(firstRender);
    }

    protected abstract string RootElementClass { get; }

    protected ElementClassBuilder ClassBuilder { get; private set; } = new ElementClassBuilder();

    protected ElementStyleBuilder StyleBuilder { get; private set; } = new ElementStyleBuilder();

    protected virtual void RegisterComponentStyles()
    {
    }

    protected virtual void RegisterComponentClasses()
    {
    }

    protected virtual void OnComponentVisibilityChanged(BitComponentVisibility visibility)
    {
    }
}
