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

            if (string.IsNullOrEmpty(docXml))
                return string.Empty;

            XElement element = XElement.Parse(docXml);

            string summary = element.Descendants().SingleOrDefault(e => e.Name.LocalName == "summary")?.Value;

            return summary != null ? "/**" + summary + "*/" : string.Empty;
        }
    }
}
