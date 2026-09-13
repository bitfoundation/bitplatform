using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bit.Butil.Build;

/// <summary>How an untrimmed publish works out which Bit.Butil types the app it is publishing uses.</summary>
public enum ButilScanMode
{
    /// <summary>Do not scan. The default: without ILLink there is then no signal but an explicit list.</summary>
    None,

    /// <summary>
    /// Match Bit.Butil type names against the names in each assembly's <c>#Strings</c> heap. Needs no table
    /// parsing at all, and over-includes whenever an app has a type of its own by the same name - which,
    /// with names like <c>Window</c>, <c>Console</c> and <c>Storage</c> in the library, is often.
    /// </summary>
    TypeNames,

    /// <summary>
    /// Match each assembly's <c>TypeRef</c> rows, which name the namespace as well, so only real references
    /// to <c>Bit.Butil</c> types count. Costs no more at publish than <see cref="TypeNames"/> and is the mode
    /// to use.
    /// </summary>
    TypeReferences,
}
