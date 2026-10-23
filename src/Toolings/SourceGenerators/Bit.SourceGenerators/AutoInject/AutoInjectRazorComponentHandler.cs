using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bit.SourceGenerators;

internal static class AutoInjectRazorComponentHandler
{
    public static string Generate(
        string classNamespace,
        string partialDeclarations,
        IReadOnlyList<AutoInjectMember> directMembers)
    {
        return BitSourceGeneratorUtil.GenerateTypeSource(classNamespace, partialDeclarations, GenerateInjectableProperties(directMembers));
    }

    private static string GenerateInjectableProperties(IReadOnlyList<AutoInjectMember> members)
    {
        var propertyNames = AutoInjectHelper.GetUniqueNames(members.Select(member => $"____{AutoInjectHelper.FormatMemberName(member.Name)}"));
        StringBuilder stringBuilder = new StringBuilder();

        for (var i = 0; i < members.Count; i++)
        {
            stringBuilder.Append(GenerateProperty(members[i].TypeDisplay, BitSourceGeneratorUtil.EscapeIdentifier(members[i].Name), propertyNames[i]));
        }

        return stringBuilder.ToString();
    }

    private static string GenerateProperty(string typeDisplay, string name, string propertyName)
    {
        return $@"
[global::System.CodeDom.Compiler.GeneratedCode(""Bit.SourceGenerators"",""{BitSourceGeneratorUtil.GetPackageVersion()}"")]
[global::System.Diagnostics.DebuggerNonUserCode]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::Microsoft.AspNetCore.Components.InjectAttribute]
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
private {typeDisplay} {propertyName} {{ get => {name}; set => {name} = value; }}
";
    }
}
