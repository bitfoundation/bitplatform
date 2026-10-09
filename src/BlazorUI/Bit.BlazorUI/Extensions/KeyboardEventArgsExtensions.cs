namespace Bit.BlazorUI;

internal static class KeyboardEventArgsExtensions
{
    /// <summary>
    /// An Escape pressed without a modifier: the only one a component claims from the surface around it (see
    /// Utils.claimEscape), so the only one it may act on - an Escape with Shift, Ctrl, Alt or Meta goes on to
    /// that surface, and a component acting on it as well would make one press do two things.
    /// </summary>
    internal static bool IsPlainEscape(this KeyboardEventArgs e)
    {
        return e.Key == "Escape" && e.ShiftKey is false && e.CtrlKey is false && e.AltKey is false && e.MetaKey is false;
    }
}
