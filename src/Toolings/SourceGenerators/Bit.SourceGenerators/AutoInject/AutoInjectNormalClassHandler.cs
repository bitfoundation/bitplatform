using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bit.SourceGenerators;

internal static class AutoInjectNormalClassHandler
{
    public static string Generate(
        string classNamespace,
        string partialDeclarations,
        string className,
        IReadOnlyList<AutoInjectMember> directMembers,
        IReadOnlyList<AutoInjectMember> baseMembers)
    {
        var sortedMembers = directMembers.OrderBy(o => o.Name, AutoInjectHelper.MemberNameComparer).ToList();

        return BitSourceGeneratorUtil.GenerateTypeSource(classNamespace, partialDeclarations, GenerateConstructor(className, sortedMembers, baseMembers));
    }

    private static string GenerateConstructor(string className, IReadOnlyList<AutoInjectMember> directMembers, IReadOnlyList<AutoInjectMember> baseMembers)
    {
        var parameters = directMembers.Select((member, index) => (Member: member, IsBase: false, Index: index))
            .Concat(baseMembers.Select((member, index) => (Member: member, IsBase: true, Index: index)))
            .OrderBy(parameter => parameter.Member.IsNullable)
            .ThenBy(parameter => parameter.Member.Name, AutoInjectHelper.MemberNameComparer)
            .ToList();

        var parameterNames = AutoInjectHelper.GetUniqueNames(parameters.Select(parameter => $"autoInjected{AutoInjectHelper.FormatMemberName(parameter.Member.Name)}"));
        var directParameterNames = new string[directMembers.Count];
        var baseParameterNames = parameterNames.Where((_, i) => parameters[i].IsBase).ToList();

        for (var i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].IsBase is false)
            {
                directParameterNames[parameters[i].Index] = parameterNames[i];
            }
        }

        var constructor = new StringBuilder();
        constructor.AppendLine($@"[global::System.CodeDom.Compiler.GeneratedCode(""Bit.SourceGenerators"",""{BitSourceGeneratorUtil.GetPackageVersion()}"")]");
        constructor.AppendLine("[global::System.Diagnostics.DebuggerNonUserCode]");
        constructor.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        constructor.Append($"public {className}(");
        constructor.Append(string.Join(",", parameters.Select((parameter, i) => $"\n    {parameter.Member.TypeDisplay} {parameterNames[i]}{(parameter.Member.IsNullable ? " = default" : string.Empty)}")));
        constructor.Append(')');

        if (baseMembers.Count > 0)
        {
            constructor.Append($"\n    : base({string.Join(", ", baseParameterNames)})");
        }

        constructor.AppendLine().AppendLine("{");

        for (var i = 0; i < directMembers.Count; i++)
        {
            constructor.AppendLine($"    {BitSourceGeneratorUtil.EscapeIdentifier(directMembers[i].Name)} = {directParameterNames[i]};");
        }

        constructor.AppendLine("}");

        return constructor.ToString();
    }
}
