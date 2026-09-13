namespace Bit.Butil;

/// <summary>How a <see cref="Navigation.Navigate"/> call should affect the history list.</summary>
public enum NavigationHistoryBehavior
{
    /// <summary>Let the browser decide: push, unless the URL is unchanged, in which case replace.</summary>
    Auto,

    /// <summary>Always add a new entry.</summary>
    Push,

    /// <summary>Always overwrite the current entry, adding nothing to the back stack.</summary>
    Replace,
}
