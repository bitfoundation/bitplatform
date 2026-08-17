using System.Reflection;
using Bit.Butil.Demo.Client.Docs;
using Bit.Butil.Demo.Server.Dtos;

namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// What using a Butil API entails beyond its signatures: which engines implement the browser API
/// underneath it, what the page has to arrange first (HTTPS, a permission prompt, a click), what
/// has to be disposed afterwards, and how the call behaves while the app is prerendering.
/// <para>
/// This is the part of a browser-API wrapper that a signature cannot state and that an agent
/// therefore gets wrong: the code compiles, the types line up, and the feature silently does
/// nothing because it was called from <c>OnInitializedAsync</c>, over plain HTTP, or outside a user
/// gesture. The support and precondition data is the same <see cref="DocsNav"/> table the site's own
/// browser-support matrix renders, and the disposal data is read off the assembly, so neither can
/// drift from what is shipped.
/// </para>
/// </summary>
public static class ButilCapabilityCatalog
{
    /// <summary>
    /// The services whose JavaScript is genuinely synchronous, so BitButil.UseFastInvoke() applies
    /// to them. Stated here because the fast path is chosen inside the library's internals and
    /// there is no public surface to reflect over; the README's "Optional fast invoke" section is
    /// the same list, written for a human.
    /// </summary>
    private static readonly string[] _fastInvokeServices = ["LocalStorage", "SessionStorage", "Cookie", "Console", "Location"];

    private static readonly Lazy<ButilCapabilityDto[]> _capabilities = new(() =>
        [.. DocsNav.ApiLinks.Select(link => new ButilCapabilityDto
        {
            Api = link.Title,
            Services = link.TypeNames(),
            BrowserSupport = link.Support.Label(),
            Requires = link.Needs.Labels(),
            Summary = link.Summary,
            DocsUrl = $"/{link.Url}"
        })]);

    /// <summary>Every documented browser API, with the engines that implement it and its preconditions.</summary>
    public static ButilCapabilityDto[] Capabilities => _capabilities.Value;

    /// <summary>
    /// Everything known about one API, resolved from a service name ("Clipboard"), a member
    /// ("Clipboard.ReadText"), a docs slug ("web-authn") or a page title.
    /// </summary>
    public static ButilApiInspectionDto Inspect(string? name)
    {
        var query = (name ?? string.Empty).Trim();

        if (query.Length == 0)
        {
            return new ButilApiInspectionDto
            {
                Query = query,
                IsKnown = false,
                Message = "Pass a Bit.Butil service name (\"Clipboard\"), a member (\"Clipboard.ReadText\"), or a docs slug (\"web-authn\")."
            };
        }

        // A member reads as the API it belongs to: "Geolocation.WatchPosition" is a question about
        // Geolocation, and answering "no such type" to it would be technically true and useless.
        var owner = query.Contains('.', StringComparison.Ordinal) ? query[..query.IndexOf('.', StringComparison.Ordinal)] : query;

        var type = ButilApiCatalog.Find(owner);
        var link = FindLink(owner, type);

        if (type is null && link is null)
        {
            var candidates = ButilApiCatalog.Types
                .Where(t => t.Name.Contains(owner, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Name)
                .Take(8)
                .ToArray();

            return new ButilApiInspectionDto
            {
                Query = query,
                IsKnown = false,
                Message = candidates.Length > 0
                    ? $"Bit.Butil has nothing called '{query}'. Did you mean: {string.Join(", ", candidates)}?"
                    : $"Bit.Butil has nothing called '{query}'. Call GetButilBrowserSupport for every documented API, or SearchButil(query: \"{query}\")."
            };
        }

        // A guide page documents no type at all; a type with no page of its own documents itself.
        string[] services = link?.TypeNames() switch
        {
            { Length: > 0 } named => named,
            _ => type is null ? [] : [type.Name]
        };

        var types = services.Select(ButilApiCatalog.Find).OfType<Type>().ToArray();

        var inject = types.Select(ButilApiCatalog.InjectLine).OfType<string>().ToArray();
        var disposables = Disposables(types);

        var nextCalls = types.Select(t => $"GetButilApiDetails(typeName: \"{t.Name}\")").ToList();
        if (link is not null) nextCalls.Add($"GetButilDocsPage(slug: \"{link.Url}\")");

        return new ButilApiInspectionDto
        {
            Query = query,
            IsKnown = true,
            Api = link?.Title ?? type!.Name,
            Services = services,
            Inject = inject.Length > 0 ? inject : null,
            BrowserSupport = link?.Support.Label() ?? "See the member's XML documentation",
            Requires = link?.Needs.Labels() ?? [],
            Notes = Notes(link, services, disposables),
            Disposables = disposables.Length > 0 ? disposables : null,
            NextCalls = [.. nextCalls]
        };
    }

    /// <summary>
    /// The combined consequences of building one feature on several APIs: the strictest hosting
    /// requirement any of them imposes, the engines that will run all of them, and the checklist.
    /// </summary>
    public static ButilFeaturePlanDto Plan(IEnumerable<string> names)
    {
        var inspections = names.Select(Inspect).ToArray();
        var known = inspections.Where(i => i.IsKnown).ToArray();

        var links = known.Select(i => DocsNav.AllLinks.FirstOrDefault(l => l.Title == i.Api))
                         .OfType<DocLink>()
                         .ToArray();

        var secure = links.Any(l => l.Needs.HasFlag(ApiNeeds.SecureContext));
        var permission = links.Any(l => l.Needs.HasFlag(ApiNeeds.Permission));
        var gesture = links.Any(l => l.Needs.HasFlag(ApiNeeds.UserGesture));
        var experimental = links.Where(l => l.Needs.HasFlag(ApiNeeds.Experimental)).ToArray();
        var limited = links.Where(l => l.Support is not ApiSupport.Broad).ToArray();
        var disposing = known.Where(i => i.Disposables is { Length: > 0 }).ToArray();

        var checklist = new List<string>
        {
            "Register the services once per DI container that renders your components with AddBitButilServices(), " +
            "and add the bit-butil.js script to the host page before the Blazor script.",

            "Call the browser from OnAfterRenderAsync or from an event handler, never from OnInitializedAsync: " +
            "during prerender there is no JS runtime, reads come back as safe defaults and void calls are no-ops."
        };

        if (secure)
        {
            checklist.Add("Serve over HTTPS (localhost counts). " +
                $"Needs a secure context: {Join(links.Where(l => l.Needs.HasFlag(ApiNeeds.SecureContext)))}.");
        }

        if (permission)
        {
            checklist.Add("Handle a denied permission as a normal outcome rather than an error - Butil returns false/null " +
                $"rather than throwing where dismissal is expected. Prompts: {Join(links.Where(l => l.Needs.HasFlag(ApiNeeds.Permission)))}. " +
                "Query the state up front with Permissions.Query so the UI can explain itself before the browser asks.");
        }

        if (gesture)
        {
            checklist.Add("Call from inside a click (or another user-initiated) handler, and do not await anything before the " +
                $"call that would lose the gesture: {Join(links.Where(l => l.Needs.HasFlag(ApiNeeds.UserGesture)))}.");
        }

        if (limited.Length > 0)
        {
            checklist.Add($"Feature-detect and provide a fallback - these do not run in every engine: {Join(limited)}. " +
                "Every wrapper that can be absent exposes IsSupported(); call it in OnAfterRenderAsync, because during " +
                "prerender a false is indistinguishable from a genuine one.");
        }

        if (experimental.Length > 0)
        {
            checklist.Add($"Treat as experimental - behind a flag or an origin trial somewhere: {Join(experimental)}. " +
                "Do not put a required path of your app behind one.");
        }

        if (disposing.Length > 0)
        {
            checklist.Add("Dispose what you open: " +
                $"{string.Join("; ", disposing.Select(i => $"{i.Api} ({string.Join(", ", i.Disposables!.Take(3))})"))}. " +
                "A subscription detaches its listener on disposal; a handle is holding a camera, a lock or a file open " +
                "until it is disposed.");
        }

        if (known.Any(i => i.Services!.Any(s => _fastInvokeServices.Contains(s, StringComparer.OrdinalIgnoreCase))))
        {
            checklist.Add("Optional: BitButil.UseFastInvoke() at startup skips the async marshalling for the synchronous " +
                $"APIs in this set ({string.Join(", ", known.SelectMany(i => i.Services!).Where(s => _fastInvokeServices.Contains(s, StringComparer.OrdinalIgnoreCase)).Distinct())}). " +
                "It is a no-op on Blazor Server and cannot affect the asynchronous APIs.");
        }

        checklist.Add("Confirm each member you call with GetButilApiDetails before writing against it - the wrappers follow " +
                      "the browser API's own naming, which is not always the name you would guess.");

        return new ButilFeaturePlanDto
        {
            Apis = inspections,
            Unknown = [.. inspections.Where(i => i.IsKnown is false).Select(i => i.Query)],
            RequiresSecureContext = secure,
            RequiresPermission = permission,
            RequiresUserGesture = gesture,
            EngineLimited = [.. limited.Select(l => $"{l.Title}: {l.Support.Label()}")],
            Checklist = [.. checklist]
        };
    }

    /// <summary>The docs entry for a name: its slug, its title, or the type it documents.</summary>
    private static DocLink? FindLink(string name, Type? type)
    {
        return DocsNav.FindByUrl(name)
            ?? DocsNav.AllLinks.FirstOrDefault(l => string.Equals(l.Title, name, StringComparison.OrdinalIgnoreCase))
            ?? DocsNav.AllLinks.FirstOrDefault(l => string.Equals(l.Title.Replace(" ", string.Empty, StringComparison.Ordinal), name, StringComparison.OrdinalIgnoreCase))
            ?? (type is null ? null : DocsNav.AllLinks.FirstOrDefault(l => l.TypeNames().Contains(type.Name, StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// The members whose result owns something the browser is holding open. Read off the assembly
    /// rather than listed: anything returning a type that implements IAsyncDisposable/IDisposable
    /// is, by construction, a thing to dispose.
    /// </summary>
    private static string[] Disposables(Type[] types)
    {
        var results = new List<string>();

        foreach (var type in types)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName) continue;

                var returned = Unwrap(method.ReturnType);

                if (returned.Assembly != typeof(BitButil).Assembly) continue;
                if (typeof(IAsyncDisposable).IsAssignableFrom(returned) is false &&
                    typeof(IDisposable).IsAssignableFrom(returned) is false) continue;

                var entry = $"{type.Name}.{method.Name} returns {ButilApiCatalog.FriendlyName(returned)}";
                if (results.Contains(entry, StringComparer.Ordinal) is false) results.Add(entry);
            }
        }

        return [.. results];
    }

    /// <summary>The value behind a Task&lt;T&gt; / ValueTask&lt;T&gt; / T?, or the type itself.</summary>
    private static Type Unwrap(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null) return Unwrap(nullable);

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();

            if (definition == typeof(Task<>) || definition == typeof(ValueTask<>)) return Unwrap(type.GetGenericArguments()[0]);
        }

        return type;
    }

    private static string[] Notes(DocLink? link, string[] services, string[] disposables)
    {
        var notes = new List<string>
        {
            "Prerendering: reads return a safe default (\"\" / [] / default(T)) and void calls are no-ops while there is " +
            "no JS runtime, so nothing throws - but a false from IsSupported() during prerender is indistinguishable " +
            "from a genuine false. Branch on a result only from OnAfterRenderAsync."
        };

        if (link is not null && link.Support is not ApiSupport.Broad and not ApiSupport.Guide)
        {
            notes.Add($"Not implemented everywhere ({link.Support.Label()}): feature-detect with IsSupported() and have a fallback path.");
        }

        if (link is not null && link.Needs.HasFlag(ApiNeeds.UserGesture))
        {
            notes.Add("The browser only honours this from inside a user gesture; awaiting something else first can spend it.");
        }

        if (link is not null && link.Needs.HasFlag(ApiNeeds.Permission))
        {
            notes.Add("A dismissed or denied prompt comes back as false/null rather than as an exception - treat it as a normal branch.");
        }

        if (disposables.Length > 0)
        {
            notes.Add("Returns something to dispose: a ButilSubscription detaches its listener, and a handle releases the " +
                      "hardware or the lock it is holding. Disposal is idempotent and safe during teardown.");
        }

        if (services.Any(s => _fastInvokeServices.Contains(s, StringComparer.OrdinalIgnoreCase)))
        {
            notes.Add("Backed by synchronous JavaScript, so BitButil.UseFastInvoke() applies on Blazor WebAssembly.");
        }

        return [.. notes];
    }

    private static string Join(IEnumerable<DocLink> links) => string.Join(", ", links.Select(l => l.Title));
}
