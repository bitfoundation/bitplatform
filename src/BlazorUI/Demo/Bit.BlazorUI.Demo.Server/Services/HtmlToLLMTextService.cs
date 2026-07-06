using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Demo.Server.Services;

public static partial class HtmlToLLMTextService
{
    public static string ToLlmFriendlyHtml(this string rawHtml)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
            return rawHtml;

        // 1. Unescape Unicode right away so HtmlAgilityPack parses the right characters
        rawHtml = Regex.Unescape(rawHtml);

        var doc = new HtmlDocument
        {
            OptionOutputOriginalCase = true
        };
        doc.LoadHtml(rawHtml);

        // 2. Loop over all remaining elements to clean attributes
        var allNodes = doc.DocumentNode.DescendantsAndSelf();
        foreach (var node in allNodes)
        {
            if (node.NodeType == HtmlNodeType.Element)
            {
                // Remove Blazor CSS isolation attributes (b-xxxxxxxxx)
                var blazorAttributes = node.Attributes
                    .Where(a => a.Name.StartsWith("b-", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var attr in blazorAttributes)
                {
                    node.Attributes.Remove(attr);
                }
            }
        }

        using var stringWriter = new StringWriter();
        doc.Save(stringWriter);

        string cleanedHtml = stringWriter.ToString();

        // 3. Decode HTML entities natively
        string friendlyHtml = WebUtility.HtmlDecode(cleanedHtml);

        return friendlyHtml.Trim();
    }
}
