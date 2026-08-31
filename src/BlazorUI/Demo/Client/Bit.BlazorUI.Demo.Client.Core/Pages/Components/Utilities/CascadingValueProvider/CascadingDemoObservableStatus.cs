using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.CascadingValueProvider;

/// <summary>
/// A state holder that reports its own mutations, which is what BitCascadingValue.Observed watches so that
/// the consumers refresh without a single call to NotifyChanged.
/// </summary>
public sealed class CascadingDemoObservableStatus : INotifyPropertyChanged
{
    private int _count;
    private string _text = "Idle";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;

            _text = value;

            OnPropertyChanged();
        }
    }

    public int Count
    {
        get => _count;
        set
        {
            if (_count == value) return;

            _count = value;

            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
