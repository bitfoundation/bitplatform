using Microsoft.AspNetCore.Components;
using Bit.Butil.Demo.Client.Pages;

namespace Bit.Butil.Demo.Client.Docs;

/// <param name="Icon">
/// The key of the mark that stands for this area of the platform, resolved by Shared/Icon.razor.
/// It is declared per group rather than per page on purpose: ninety-one glyphs in one list is
/// decoration a reader has to look past to find a name, while eleven of them are landmarks that say
/// which part of the browser they are now in.
/// </param>
public record DocGroup(string Title, string Icon, DocLink[] Links);
