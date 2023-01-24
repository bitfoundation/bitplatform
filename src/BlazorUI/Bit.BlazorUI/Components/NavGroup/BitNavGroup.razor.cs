﻿using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

public partial class BitNavGroup : IDisposable
{
    private bool SelectedKeyHasBeenSet;
    private string? selectedKey;

    private IList<BitNavOption> _options = new List<BitNavOption>();

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;

    /// <summary>
    /// A list of options to render as children of the current option
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The initially selected option in manual mode.
    /// </summary>
    [Parameter] public string? DefaultSelectedKey { get; set; }

    /// <summary>
    /// Used to customize how content inside the group header is rendered.
    /// </summary>
    [Parameter] public RenderFragment<BitNavOption>? HeaderTemplate { get; set; }

    /// <summary>
    /// Used to customize how content inside the link tag is rendered.
    /// </summary>
    [Parameter] public RenderFragment<BitNavOption>? OptionTemplate { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// The default value is Automatic.
    /// </summary>
    [Parameter] public BitNavGroupMode Mode { get; set; } = BitNavGroupMode.Automatic;

    /// <summary>
    /// Callback invoked when an option is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitNavOption> OnOptionClick { get; set; }

    /// <summary>
    /// Callback invoked when an option is selected.
    /// </summary>
    [Parameter] public EventCallback<BitNavOption> OnSelectOption { get; set; }

    /// <summary>
    /// Callback invoked when a group header is clicked and Expanded or Collapse.
    /// </summary>
    [Parameter] public EventCallback<BitNavOption> OnOptionToggle { get; set; }

    /// <summary>
    /// The way to render nav links.
    /// </summary>
    [Parameter] public BitNavGroupRenderType RenderType { get; set; } = BitNavGroupRenderType.Normal;

    /// <summary>
    /// Selected option to show in Nav.
    /// </summary>
    [Parameter]
    public string? SelectedKey
    {
        get => selectedKey;
        set
        {
            if (value == selectedKey) return;
            selectedKey = value;
            SelectedKeyChanged.InvokeAsync(value);
            ExpandParents(_options);
        }
    }
    [Parameter] public EventCallback<string> SelectedKeyChanged { get; set; }

    protected override string RootElementClass => "bit-nvg";

    protected override async Task OnInitializedAsync()
    {
        if (Mode == BitNavGroupMode.Automatic)
        {
            _navigationManager.LocationChanged += OnLocationChanged;

            SelectOptionByCurrentUrl();
        }
        else if (DefaultSelectedKey is not null && SelectedKeyHasBeenSet is false)
        {
            SelectedKey = DefaultSelectedKey;
        }

        await base.OnInitializedAsync();
    }

    private static List<BitNavOption> Flatten(IList<BitNavOption> e) => e.SelectMany(c => Flatten(c._options)).Concat(e).ToList();

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SelectOptionByCurrentUrl();

        StateHasChanged();
    }

    private void SelectOptionByCurrentUrl()
    {
        var currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        var currentOption = Flatten(_options).FirstOrDefault(option => option.Url == currentUrl);

        SelectedKey = currentOption?.Key;
    }

    private bool ExpandParents(IList<BitNavOption> options)
    {
        foreach (var option in options)
        {
            if (option.Key == SelectedKey || (option._options.Any() && ExpandParents(option._options))) 
            {
                option.SetIsExpanded(true);
                return true;
            }
        }

        return false;
    }

    internal async Task HandleOnClick(BitNavOption option)
    {
        if (option.IsEnabled == false) return;

        await OnOptionClick.InvokeAsync(option);

        if (option._options.Any() && option.Url.HasNoValue())
        {
            await ToggleOption(option);
        }
        else if (Mode == BitNavGroupMode.Manual)
        {
            SelectedKey = option.Key;

            await OnSelectOption.InvokeAsync(option);

            StateHasChanged();
        }
    }

    internal async Task ToggleOption(BitNavOption option)
    {
        if (option.IsEnabled is false || option._options.Any() is false) return;

        option.SetIsExpanded(!option.IsExpanded);

        await OnOptionClick.InvokeAsync(option);
    }

    internal void RegisterOptions(BitNavOption option)
    {
        if (option.Key.HasNoValue())
        {
            option.SetKey($"{_options.Count}");
        }
        _options.Add(option);
        StateHasChanged();
    }

    internal void UnregisterOptions(BitNavOption option)
    {
        _options.Remove(option);
        StateHasChanged();
    }

    internal void RegisterChildOptions(BitNavOption parent, BitNavOption option)
    {
        if (option.Key.HasNoValue())
        {
            option.SetKey($"{parent.Key}-{parent._options.Count}");
        }
        Flatten(_options).FirstOrDefault(i => i == parent)?._options.Add(option);
        StateHasChanged();
    }

    internal void UnregisterChildOptions(BitNavOption parent, BitNavOption option)
    {
        Flatten(_options).FirstOrDefault(i => i == parent)?._options.Remove(option);
        StateHasChanged();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Mode == BitNavGroupMode.Automatic)
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
