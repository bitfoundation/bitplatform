using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis
{
    public static class ISymbolExtensions
    {
        public static string GetDocumentationSummary(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            string? docXml = symbol.GetDocumentationCommentXml();
            string? summary = null;

            if (!string.IsNullOrEmpty(docXml))
            {
                XElement element = XElement.Parse(docXml);

                summary = element.Descendants().ExtendedSingleOrDefault($"Looking for summary of {symbol.Name}", e => e.Name.LocalName == "summary")?.Value;
            }

            if (symbol is IFieldSymbol field)
            {
                if (field.HasConstantValue)
                {
                    if (summary == null)
                        summary = string.Empty;
                    else
                        summary += Environment.NewLine;
                    summary += $"Value: {field.ConstantValue}";
                }
            }

            return !string.IsNullOrEmpty(summary) ? "/**" + summary + "*/" : string.Empty;
        }

        public static bool IsOperation(this IMethodSymbol methodSymbol, out AttributeData operationAttribute)
        {
            if (methodSymbol == null)
                throw new ArgumentNullException(nameof(methodSymbol));

            ImmutableArray<AttributeData> attrs = methodSymbol.GetAttributes();

            operationAttribute = attrs.ExtendedSingleOrDefault($"Looking for action/function attribute on {methodSymbol.Name}", att => att.AttributeClass.Name == "ActionAttribute" || att.AttributeClass.Name == "FunctionAttribute");

            return operationAttribute != null;
        }
    }
}
