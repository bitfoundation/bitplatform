using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Bit.BlazorUI.Build;

/// <summary>A type of one of the library assemblies: what it reaches, and the string literals its code loads.</summary>
public sealed class TypeNode
{
    internal TypeNode(LibraryAssembly assembly, TypeDefinitionHandle handle, string fullName, string name)
    {
        Assembly = assembly;
        Handle = handle;
        FullName = fullName;
        Name = name;
    }

    public LibraryAssembly Assembly { get; }

    internal TypeDefinitionHandle Handle { get; }

    /// <summary>"Bit.BlazorUI.BitDropdown`2", "Bit.BlazorUI.BitDropdown`2/&lt;OnInitializedAsync&gt;d__12" for a nested one.</summary>
    public string FullName { get; }

    /// <summary>The name a csproj and a bit-trim comment use for it: no namespace, no arity ("BitDropdown").</summary>
    public string Name { get; }

    internal List<TypeNode>? Edges { get; set; }

    internal List<string>? Strings { get; set; }

    public override string ToString() => $"{Assembly.Name}:{FullName}";
}
