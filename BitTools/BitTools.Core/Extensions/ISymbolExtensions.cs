using System;
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

            string docXml = symbol.GetDocumentationCommentXml();
            string summary = null;

            if (!string.IsNullOrEmpty(docXml))
            {
                XElement element = XElement.Parse(docXml);

                summary = element.Descendants().ExtendedSingleOrDefault($"Looking for summary of {symbol.Name}", e => e.Name.LocalName == "summary")?.Value;
            }

            if (symbol is IFieldSymbol)
            {
                IFieldSymbol field = (IFieldSymbol)symbol;
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
    }
}
