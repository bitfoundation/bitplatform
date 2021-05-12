﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Bit.Tooling.SourceGenerators
{
    [Generator]
    public class BlazorSetParametersSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not BlazorParameterPropertySyntaxReceiver receiver)
                return;

            foreach (IGrouping<ISymbol, IPropertySymbol> group in receiver.Properties.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
            {
                string classSource = GeneratePartialClassToOverrideSetParameters((INamedTypeSymbol)group.Key, group.ToList());
                context.AddSource($"{group.Key.Name}_SetParametersAsync.AutoGenerated.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new BlazorParameterPropertySyntaxReceiver());
        }

        private string GeneratePartialClassToOverrideSetParameters(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties)
        {
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            StringBuilder source = new StringBuilder($@"using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;
using System;

namespace {namespaceName}
{{
    public partial class {classSymbol.Name}  
    {{
        public override Task SetParametersAsync(ParameterView parameters) 
        {{
            foreach (ParameterValue parameter in parameters)
            {{
                switch (parameter.Name)
                {{
");

            // create cases for each property 
            foreach (IPropertySymbol propertySymbol in properties)
            {
                GenerateParameterReaderCode(source, propertySymbol);
            }

            source.AppendLine("                }");
            source.AppendLine("            }");

            if (classSymbol.BaseType.ToDisplayString() == "Microsoft.AspNetCore.Components.ComponentBase")
            {
                source.AppendLine("            return base.SetParametersAsync(ParameterView.Empty);");
            }
            else
            {
                source.AppendLine("            return base.SetParametersAsync(parameters);");
            }

            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine("}");

            return source.ToString();
        }

        private void GenerateParameterReaderCode(StringBuilder source, IPropertySymbol propertySymbol)
        {
            source.AppendLine($"                    case nameof({propertySymbol.Name}):");
            source.AppendLine($"                       {propertySymbol.Name} = ({propertySymbol.Type.ToDisplayString()})parameter.Value;");
            source.AppendLine("                       break;");
        }
    }
}
