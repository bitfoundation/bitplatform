using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bit.Butil.Build;

/// <summary>Module names in dependency-first order, and each module's direct dependencies.</summary>
public sealed class ButilScriptManifest(IReadOnlyList<string> order, IReadOnlyDictionary<string, string[]> dependencies)
{
    public IReadOnlyList<string> Order { get; } = order;

    public IReadOnlyDictionary<string, string[]> Dependencies { get; } = dependencies;
}
