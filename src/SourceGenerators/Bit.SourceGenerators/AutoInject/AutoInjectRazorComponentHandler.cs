using System.Collections.Generic;
using System.Text;

namespace Bit.SourceGenerators;

internal static class AutoInjectRazorComponentHandler
{
    public static string? Generate(
        string classNamespace,
        string classNameForCode,
        IReadOnlyCollection<AutoInjectMember> directMembers)
    {
        string source = $@"
using Microsoft.AspNetCore.Components;
using System.ComponentModel;

namespace {classNamespace}
{{
    public partial class {classNameForCode}
    {{
        {GenerateInjectableProperties(directMembers)}
    }}
}}";
        return source;
    }

    private static string GenerateInjectableProperties(IReadOnlyCollection<AutoInjectMember> members)
    {
        StringBuilder stringBuilder = new StringBuilder();

        foreach (var member in members)
        {
            stringBuilder.Append(GenerateProperty(member.TypeDisplay, member.Name));
        }

        return stringBuilder.ToString();
    }

    private static string GenerateProperty(string typeDisplay, string name)
    {
        return $@"
        [global::System.CodeDom.Compiler.GeneratedCode(""Bit.SourceGenerators"",""{BitSourceGeneratorUtil.GetPackageVersion()}"")]
        [global::System.Diagnostics.DebuggerNonUserCode]
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
{"\t\t"}[Inject]
{"\t\t"}[EditorBrowsable(EditorBrowsableState.Never)]
{"\t\t"}private {typeDisplay} ____{AutoInjectHelper.FormatMemberName(name)} {{ get => {name}; set => {name} = value; }}";
    }
}
