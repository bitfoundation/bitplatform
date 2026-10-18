using Microsoft.AspNetCore.Components;
using Bit.Butil.Demo.Client.Pages;

namespace Bit.Butil.Demo.Client.Docs;

/// <param name="Icon">
/// The key of the mark that stands for this area of the platform, resolved by Shared/Icon.razor.
/// It is declared per group rather than per page on purpose: a glyph per page is decoration a
/// reader has to look past to find a name, while a glyph per area is a landmark that says which
/// part of the browser they are now in.
/// </param>
public record DocGroup(string Title, string Icon, DocLink[] Links);
