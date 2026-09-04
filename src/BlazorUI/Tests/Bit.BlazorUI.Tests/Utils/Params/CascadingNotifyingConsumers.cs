using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Utils.Params;

/// <summary>
/// A state holder that reports its own property changes, which is what BitCascadingValue.AutoNotify watches.
/// </summary>
public sealed class NotifyingCascadingState : INotifyPropertyChanged
{
    private string _text = "initial";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;

            _text = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    /// <summary>
    /// Whether anything is currently watching this state, which is what tells a cascading value that let go
    /// of its subscription apart from one that is still holding it.
    /// </summary>
    public bool HasSubscribers => PropertyChanged is not null;
}

public sealed class NotifyingStateConsumer : ComponentBase
{
    [CascadingParameter] public NotifyingCascadingState? State { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, State?.Text ?? "none");
    }
}

public sealed class ObservableNamesConsumer : ComponentBase
{
    [CascadingParameter] public ObservableCollection<string>? Names { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, Names is null ? "none" : string.Join(",", Names));
    }
}

/// <summary>
/// Reads two differently named cascading values of the same type, which is what tells apart a consumer that
/// was matched again under a new name from one that is still bound to the supplier of the old one.
/// </summary>
public sealed class DualNameConsumer : ComponentBase
{
    [CascadingParameter(Name = "First")] public string? First { get; set; }

    [CascadingParameter(Name = "Second")] public string? Second { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, $"{First ?? "none"}-{Second ?? "none"}");
    }
}
