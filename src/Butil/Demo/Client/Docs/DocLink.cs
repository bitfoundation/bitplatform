using Microsoft.AspNetCore.Components;
using Bit.Butil.Demo.Client.Pages;

namespace Bit.Butil.Demo.Client.Docs;

/// <param name="PageType">
/// The component routed at <paramref name="Url"/>. Naming it here is what lets the MCP server
/// (Server/Controllers/McpController.cs) render a page's documentation on demand, so an agent reads
/// the same text a human does instead of a second copy that could go stale.
/// </param>
/// <param name="Services">
/// The Bit.Butil public types the page documents, when they are not simply the title without its
/// spaces. Only the pages whose title is not a type name ("Local &amp; Session Storage") or whose
/// API is a set of extension methods ("Element", "Animation") need to state them.
/// </param>
public record DocLink(
    string Title,
    string Url,
    string Summary,
    Type PageType,
    ApiSupport Support = ApiSupport.Broad,
    ApiNeeds Needs = ApiNeeds.None,
    string[]? Services = null);
