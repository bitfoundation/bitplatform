using Bit.BlazorUI.Demo.Shared.Dtos;
using Bit.BlazorUI.Demo.Shared.Dtos.SiteSearch;

namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class SiteCommandPalette
{
    /// <summary>The component pages and the guides, read off the nav once: it is the same for every visitor.</summary>
    private static readonly Lazy<BitCommandPaletteCommand[]> _navCommands = new(BuildNavCommands);

    private BitCommandPaletteCommand[] _commands = [];
    private IDisposable? _sourceRegistration;

    [AutoInject] private HttpClient _httpClient = default!;
    [AutoInject] private IBitCommandPaletteService _paletteService = default!;

    protected override Task OnInitAsync()
    {
        _commands = _navCommands.Value;

        // Registered rather than passed in: a source is the palette's way to search what it does not hold, and the
        // sections are fetched from the server only once somebody types.
        _sourceRegistration = _paletteService.RegisterSource(new SiteSectionSource(_httpClient));

        return base.OnInitAsync();
    }

    private static BitCommandPaletteCommand[] BuildNavCommands()
    {
        var commands = new List<BitCommandPaletteCommand>();

        foreach (var item in ComponentCatalog.Items)
        {
            commands.Add(new()
            {
                Id = $"site-component-{item.Url}",
                Title = item.Name,
                Subtitle = item.Category,
                Description = item.Summary,
                Group = "Components",
                Keywords = [.. Words(item.Aliases), .. Words(item.Keywords)],
                Href = item.Url,
                IconCss = $"bit-icon bit-icon--{item.IconName}",
            });
        }

        // The guides: every page of the nav that is not a component page (getting started, theming, the AI
        // scenarios, ...).
        foreach (var (item, parent) in Leaves(MainLayout.NavItems, null))
        {
            if (item.Url is not { Length: > 0 } url || url.StartsWith("/components/", StringComparison.OrdinalIgnoreCase)) continue;

            commands.Add(new()
            {
                Id = $"site-guide-{url}",
                Title = item.Text ?? url,
                Subtitle = parent,
                Description = item.Description,
                Group = "Guides",
                Href = url,
                IconCss = item.IconName is { Length: > 0 } icon ? $"bit-icon bit-icon--{icon}" : null,
            });
        }

        return [.. commands];
    }

    private static IEnumerable<(BitNavItem Item, string? Parent)> Leaves(IEnumerable<BitNavItem> items, string? parent)
    {
        foreach (var item in items)
        {
            if (item.ChildItems is { Count: > 0 } children)
            {
                foreach (var leaf in Leaves(children, item.Text)) yield return leaf;
            }
            else
            {
                yield return (item, parent);
            }
        }
    }

    private static IEnumerable<string> Words(string? list)
        => list?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            _sourceRegistration?.Dispose();
            _sourceRegistration = null;
        }

        await base.DisposeAsync(disposing);
    }



    /// <summary>
    /// Every topic and example section of the component pages, searched in memory once fetched. Asked only once something
    /// is typed: three thousand sections listed under an empty box would bury the pages themselves.
    /// </summary>
    private sealed class SiteSectionSource(HttpClient httpClient) : IBitCommandPaletteSource
    {
        // One download per app (per process on the server): the list is the same for everybody and never changes while
        // the site runs. A failed one is forgotten, so the next search tries again.
        private static Task<List<SiteSearchSectionDto>>? _sections;

        public string Name => "Examples";

        public int MinQueryLength => 2;

        public int MaxResults => 25;

        // The palette's own matcher ranks what is found here together with the pages, so "dropdown" lists the
        // Dropdown page above its sections.
        public bool ApplyMatcher => true;

        public async Task<IReadOnlyList<BitCommandPaletteCommand>> SearchAsync(string query, BitCommandPaletteSourceContext context, CancellationToken cancellationToken)
        {
            List<SiteSearchSectionDto> sections;
            var load = _sections ??= Load(httpClient);

            try
            {
                sections = await load.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // This search was superseded; the download itself goes on for the next one.
                throw;
            }
            catch
            {
                // The download failed (an HttpClient timeout is a cancellation too). Forgotten only if no other search
                // has started a new one meanwhile.
                Interlocked.CompareExchange(ref _sections, null, load);
                return [];
            }

            var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // A first cut for the matcher: every term somewhere in the title, the component or the topic.
            return [.. sections.Where(s => terms.All(t => s.Title.Contains(t, StringComparison.OrdinalIgnoreCase)
                                                       || s.Component.Contains(t, StringComparison.OrdinalIgnoreCase)
                                                       || s.Topic?.Contains(t, StringComparison.OrdinalIgnoreCase) is true))
                               .Take(200)
                               .Select(s => new BitCommandPaletteCommand
                               {
                                   Id = $"site-section-{s.Url}",
                                   Title = s.Title,
                                   Subtitle = s.Topic is null ? s.Component : $"{s.Component} · {s.Topic}",
                                   Keywords = s.Topic is null ? [s.Component] : [s.Component, s.Topic],
                                   Href = s.Url,
                                   // A section of a page is a place to go, not something to keep in the recents above
                                   // the pages themselves.
                                   ExcludeFromHistory = true,
                               })];
        }

        private static async Task<List<SiteSearchSectionDto>> Load(HttpClient httpClient)
            => await httpClient.GetFromJsonAsync("api/SiteSearch/GetSections", AppJsonContext.Default.ListSiteSearchSectionDto) ?? [];
    }
}
