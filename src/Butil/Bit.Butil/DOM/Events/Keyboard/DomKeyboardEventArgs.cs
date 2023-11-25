using System;

namespace Bit.Butil;

public class DomKeyboardEventArgs : EventArgs
{
    internal static readonly string[] SelectedMembers = ["code"];

    public string Code { get; set; } = string.Empty;
}
