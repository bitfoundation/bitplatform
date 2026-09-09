namespace Bit.BlazorUI;

/// <summary>
/// The set of buttons a <see cref="BitMessageBox"/> renders in its footer.
/// </summary>
/// <remarks>
/// The sets are the ones a message box has always been asked in - the same four
/// <c>MessageBoxButton</c> combinations a desktop message box offers - so that the answer a
/// caller gets back is one of a handful of known values rather than an index into a list of
/// labels. The words on the buttons are still the caller's:
/// <see cref="BitMessageBox.OkText"/>, <see cref="BitMessageBox.CancelText"/>,
/// <see cref="BitMessageBox.YesText"/> and <see cref="BitMessageBox.NoText"/> name them.
/// </remarks>
public enum BitMessageBoxButtons
{
    /// <summary>
    /// A single Ok button, which answers with <see cref="BitMessageBoxResult.Ok"/>.
    /// </summary>
    Ok,

    /// <summary>
    /// An Ok and a Cancel button, which answer with <see cref="BitMessageBoxResult.Ok"/> and
    /// <see cref="BitMessageBoxResult.Cancel"/>.
    /// </summary>
    OkCancel,

    /// <summary>
    /// A Yes and a No button, which answer with <see cref="BitMessageBoxResult.Yes"/> and
    /// <see cref="BitMessageBoxResult.No"/>.
    /// </summary>
    YesNo,

    /// <summary>
    /// A Yes, a No and a Cancel button.
    /// </summary>
    YesNoCancel
}
