namespace Bit.Blow;

/// <summary>Where a <see cref="BlowGraphChange"/> came from.</summary>
public enum BlowChangeOrigin
{
    /// <summary>The user, with a pointer gesture or a key on the canvas.</summary>
    User,

    /// <summary>The application, through the canvas's API.</summary>
    Api,

    /// <summary>The built-in history: an undo or a redo.</summary>
    History,

    /// <summary>
    /// Somewhere else - another user's edit applied by a collaboration adapter, a server push. Passed to
    /// <c>BlowCanvas.EditAsync</c> by the code applying it, so a listener that broadcasts local changes can tell
    /// the ones not to send back.
    /// </summary>
    Remote,
}
