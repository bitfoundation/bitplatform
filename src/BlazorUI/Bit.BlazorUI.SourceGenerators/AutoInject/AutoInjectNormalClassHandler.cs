using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bit.SourceGenerators;

namespace Bit.BlazorUI.SourceGenerators.AutoInject;

internal static class AutoInjectNormalClassHandler
{
    public static string? Generate(
        string classNamespace,
        string classNameForCode,
        string className,
        IReadOnlyCollection<AutoInjectMember> directMembers,
        IReadOnlyCollection<AutoInjectMember> baseMembers)
    {
        var sortedMembers = directMembers.OrderBy(o => o.Name).ToList();

        string source = $@"
namespace {classNamespace}
{{
    public partial class {classNameForCode}
    {{
        {GenerateConstructor(className, sortedMembers, baseMembers)}
    }}
}}";
        return source;
    }

    private static string GenerateConstructor(string className, IReadOnlyCollection<AutoInjectMember> directMembers, IReadOnlyCollection<AutoInjectMember> baseMembers)
    {
        string generateConstructor = $@"
        [global::System.CodeDom.Compiler.GeneratedCode(""Bit.SourceGenerators"",""{BitSourceGeneratorUtil.GetPackageVersion()}"")]
        [global::System.Diagnostics.DebuggerNonUserCode]
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
{"\t\t"}public {className}({GenerateConstructorParameters(directMembers, baseMembers)}){PassParametersToBaseClass(baseMembers)}
{"\t\t"}{{
{AssignMembersFromParameters(directMembers)}
{"\t\t"}}}
";
        return generateConstructor;
    }

    private static string PassParametersToBaseClass(IReadOnlyCollection<AutoInjectMember> baseMembers)
    {
        if (baseMembers.Any() is false)
            return string.Empty;

        StringBuilder baseConstructor = new();
        baseConstructor.Append(": base(");

        foreach (var member in baseMembers)
        {
            baseConstructor.Append($@"{'\n'}{"\t\t\t\t\t\t"}autoInjected{AutoInjectHelper.FormatMemberName(member.Name)},");
        }

        baseConstructor.Length--;
        baseConstructor.Append(')');

        return baseConstructor.ToString();
    }

    private static string AssignMembersFromParameters(IReadOnlyCollection<AutoInjectMember> directMembers)
    {
        StringBuilder stringBuilder = new();
        foreach (var member in directMembers)
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append('\n');
            }
            stringBuilder.Append("\t\t\t")
                .Append($@"{member.Name} = autoInjected{AutoInjectHelper.FormatMemberName(member.Name)};");
        }

        return stringBuilder.ToString();
    }

    private static string GenerateConstructorParameters(IReadOnlyCollection<AutoInjectMember> directMembers, IReadOnlyCollection<AutoInjectMember> baseMembers)
    {
        StringBuilder stringBuilder = new();
        var allMembers = directMembers.Concat(baseMembers).OrderBy(o => o.Name).ToList();

        foreach (var member in allMembers)
        {
            var nullValue = member.IsNullable ? " = null" : string.Empty;
            stringBuilder.Append($@"{'\n'}{"\t\t\t"}{member.TypeDisplay} autoInjected{AutoInjectHelper.FormatMemberName(member.Name)}{nullValue},");
        }

        stringBuilder.Length--;

        return stringBuilder.ToString();
    }
}

