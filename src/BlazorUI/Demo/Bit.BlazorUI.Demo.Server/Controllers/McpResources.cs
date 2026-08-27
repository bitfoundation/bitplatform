using System.ComponentModel;
using ModelContextProtocol.Server;
using Microsoft.AspNetCore.Components.Web;
using Bit.BlazorUI.Demo.Server.Services.Mcp;

namespace Bit.BlazorUI.Demo.Server.Controllers;

/// <summary>
/// The same body of knowledge the tools serve, exposed as MCP resources.
/// <para>
/// Tools are for an agent that has decided what it needs; resources are for a client that wants to
/// attach documentation to a conversation up front, or let a person browse and pin it. Both read
/// the same catalogs, so neither can go stale relative to the other.
/// </para>
/// <para>
/// Each one carries a slug for its Name and a sentence for its Title, which is the split the
/// protocol asks for and the split a resource picker needs: the name is the identifier a client
/// stores and a completion returns, and has to stay the same across releases, while the title is
/// the line a person reads in the list and is free to be rewritten whenever it reads better.
/// </para>
/// </summary>
[McpServerResourceType]
public class McpResources(HtmlRenderer htmlRenderer, ILogger<McpResources> logger)
{
    [McpServerResource(UriTemplate = "bitblazorui://components", Name = "bitblazorui-components",
                       Title = "Every bit BlazorUI component", MimeType = "text/markdown")]
    [Description("The catalog of every component, grouped by category, with the package it ships in, the names other libraries use for it and a line on what it is for.")]
    public static string Components() => BlazorUIMarkdown.ComponentCatalog();

    [McpServerResource(UriTemplate = "bitblazorui://components/{name}", Name = "bitblazorui-component",
                       Title = "One component's API", MimeType = "text/markdown")]
    [Description("The full reference of one component - every parameter with its type, default and description, its own classes and enums, and the titles of its examples. E.g. bitblazorui://components/BitDropdown.")]
    public static string Component(string name)
    {
        var component = BlazorUIComponentCatalog.Find(name);

        return component is null ? $"bit BlazorUI has no component called '{name}'." : BlazorUIMarkdown.Component(component);
    }

    [McpServerResource(UriTemplate = "bitblazorui://components/{name}/examples", Name = "bitblazorui-component-examples",
                       Title = "One component's worked examples", MimeType = "text/markdown")]
    [Description("The Razor and C# of every worked example on a component's documentation page. E.g. bitblazorui://components/BitDataGrid/examples.")]
    public static string Examples(string name)
    {
        var component = BlazorUIComponentCatalog.Find(name);

        return component is null ? $"bit BlazorUI has no component called '{name}'." : BlazorUIMarkdown.Examples(component, filter: null);
    }

    [McpServerResource(UriTemplate = "bitblazorui://types/{typeName}", Name = "bitblazorui-type",
                       Title = "One type's reference", MimeType = "text/markdown")]
    [Description("The full reference of one public type - an enum's values, a class's members, a service's methods. E.g. bitblazorui://types/BitColor.")]
    public static string Type(string typeName)
    {
        var type = BlazorUITypeCatalog.Find(typeName);

        return type is null ? $"bit BlazorUI has no public type called '{typeName}'." : BlazorUIMarkdown.Type(type);
    }

    [McpServerResource(UriTemplate = "bitblazorui://setup/{hostingModel}", Name = "bitblazorui-setup",
                       Title = "Setup for one hosting model", MimeType = "text/markdown")]
    [Description("Everything needed to add bit BlazorUI to an app in one hosting model. E.g. bitblazorui://setup/web-app.")]
    public static string Setup(string hostingModel)
        => BlazorUISetupGuide.Get(hostingModel) ?? $"'{hostingModel}' is not a known hosting model. Use one of: {string.Join(", ", BlazorUISetupGuide.HostingModels)}.";

    /// <summary>
    /// The one answer here that is not narrowed to a chapter: a resource is attached by a person who
    /// asked for the theming reference, rather than pulled by a model mid-turn, and "the index and
    /// what each chapter covers" is what that person is choosing from.
    /// </summary>
    [McpServerResource(UriTemplate = "bitblazorui://theming", Name = "bitblazorui-theming",
                       Title = "The theming reference", MimeType = "text/markdown")]
    [Description("The index of the theming reference: what a theme in this library is, and what each chapter covers.")]
    public async Task<string> Theming() => await BlazorUIThemingGuide.Get(htmlRenderer, logger, section: null);

    [McpServerResource(UriTemplate = "bitblazorui://theming/{section}", Name = "bitblazorui-theming-section",
                       Title = "One chapter of the theming reference", MimeType = "text/markdown")]
    [Description("One chapter of the theming reference. E.g. bitblazorui://theming/Design%20tokens.")]
    public async Task<string> ThemingSection(string section) => await BlazorUIThemingGuide.Get(htmlRenderer, logger, section);
}
