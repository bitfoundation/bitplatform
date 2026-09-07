using System.Reflection;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Iconography;

/// <summary>
/// One icon of the Fabric (MDL2) set, with everything the page shows about it and everything a
/// reader can paste out of it.
/// </summary>
public sealed record IconEntry(string FieldName, string Name)
{
    /// <summary>
    /// What the icon is called in C#. Not always <c>BitIconName.{Name}</c>: the fields for the
    /// handful of names that start with a digit carry a leading underscore.
    /// </summary>
    public string Constant => $"BitIconName.{FieldName}";

    public string CssClass => $"bit-icon bit-icon--{Name}";

    public string RazorIconName => $"IconName=\"@BitIconName.{FieldName}\"";

    public string RazorIconInfo => $"Icon=\"@BitIconInfo.Bit(\"{Name}\")\"";

    /// <summary>
    /// Whether this glyph is one of the two dozen embedded in the core package's own font subset,
    /// and so renders without <c>Bit.BlazorUI.Icons</c> being installed at all. It is the one fact
    /// about an icon that changes what a reader has to ship, so the panel leads with it.
    /// </summary>
    public bool InCorePackage { get; init; }

    /// <summary>The whole name, lower-cased once, for the substring passes of the search.</summary>
    public string Lower { get; init; } = string.Empty;

    /// <summary>
    /// The name split at its capitals and lower-cased - "AddFriend" as ["add", "friend"]. Words
    /// rather than substrings is what keeps "add" out of "Address", and lets a category be stated
    /// as the handful of words that name it instead of as a list of fragments that half-match.
    /// </summary>
    public string[] Words { get; init; } = [];

    /// <summary>One bit per entry of <see cref="IconCatalog.Categories"/>.</summary>
    internal int CategoryMask { get; init; }
}


/// <summary>
/// The Fabric (MDL2) set as the iconography page needs it: named, categorized, searchable, and
/// built once for the life of the app rather than once per visit.
/// <para>
/// Nothing here is a second copy of the set. The names come off <see cref="BitIconName"/> by
/// reflection, and everything else - the words, the categories, the ranking - is derived from
/// those names. An icon added to the font and to <c>BitIconName</c> turns up here with no edit.
/// </para>
/// <para>
/// The categories are the one judgement call. MDL2 ships no taxonomy, so they are inferred from
/// the words in each name, which makes them a fast way in rather than a specification: an icon can
/// sit in two of them and a few sit in none. That is why the browser opens on all of them and the
/// chips only ever narrow.
/// </para>
/// </summary>
public static class IconCatalog
{
    /// <summary>
    /// The glyphs the CORE package embeds - the 3 KB font subset its own components draw with,
    /// which is why a BitDatePicker has arrows in an app that never installed the icons package.
    /// <para>
    /// Copied from <c>Bit.BlazorUI/Styles/fabric.mdl2.bit.blazoui.scss</c>, which is the source of
    /// truth. Deriving it instead would mean fetching and scanning the 800 KB core stylesheet on
    /// every visit to read twenty-five names out of it.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> coreGlyphs = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept", "Add", "CalendarMirrored", "Cancel", "ChevronDownSmall", "ChevronRight",
        "ChromeBackMirrored", "Clock", "Completed", "Delete", "DoubleChevronUp", "ErrorBadge",
        "FavoriteStar", "FavoriteStarFill", "GotoToday", "Hide3", "Info", "More", "Pause", "Play",
        "Search", "Sort", "Up", "View", "Warning"
    };

    /// <summary>
    /// The categories, and the words that put an icon in one. A keyword matches a whole word of a
    /// name, never a fragment of one, and its plural is added for free when the catalog is built.
    /// </summary>
    private static readonly (string Name, string[] Keywords)[] categoryDefinitions =
    [
        ("Actions", ["add", "edit", "delete", "remove", "save", "copy", "paste", "cut", "undo", "redo",
                     "refresh", "sync", "share", "download", "upload", "cancel", "accept", "clear",
                     "search", "filter", "sort", "pin", "unpin", "print", "import", "export", "rename",
                     "open", "send", "reply", "move", "select", "install", "uninstall", "update", "zoom",
                     "hide", "show", "view", "attach", "favorite", "like", "follow", "subscribe",
                     "checkmark", "clone", "revert", "repair", "settings", "merge", "split"]),

        ("Arrows", ["arrow", "chevron", "caret", "triangle", "scroll", "expand", "collapse", "back",
                    "forward", "next", "previous", "up", "down", "left", "right", "return", "drill",
                    "drilldown", "drillup", "navigate", "navigation", "direction"]),

        ("Charts & data", ["chart", "graph", "analytics", "diagram", "table", "database", "metric",
                           "trending", "pie", "bar", "donut", "scatter", "histogram", "kpi", "dashboard",
                           "data", "insight", "pivot", "funnel", "report", "column", "row", "cell",
                           "query", "sql", "powerbi", "forecast", "calculator"]),

        ("Commerce", ["shop", "shopping", "cart", "money", "payment", "currency", "price", "pricing",
                      "invoice", "billing", "bank", "coupon", "gift", "store", "buy", "sell", "sale",
                      "order", "receipt", "wallet", "credit", "dollar", "euro", "yen", "pound", "ruble",
                      "cash", "checkout", "tag", "market", "product", "offer", "deal", "subscription",
                      "budget"]),

        ("Communication", ["mail", "email", "message", "chat", "comment", "phone", "call", "inbox",
                           "outbox", "contact", "ringer", "notification", "feedback", "conversation",
                           "teams", "skype", "sms", "voicemail", "forum", "announcement", "megaphone",
                           "broadcast", "reply", "send", "envelope", "dial", "headset"]),

        ("Design", ["color", "brush", "palette", "canvas", "crop", "rotate", "flip", "resize", "opacity",
                    "gradient", "shadow", "theme", "design", "ink", "pen", "pencil", "eyedropper",
                    "ruler", "artboard", "layer", "mirror", "shape", "circle", "square", "star", "heart",
                    "diamond", "emoji", "symbol", "ring", "hexagon", "line", "sketch", "art", "paint",
                    "stroke", "fill", "border", "contrast", "brightness", "saturation"]),

        ("Development", ["code", "developer", "terminal", "bug", "branch", "repo", "repository", "git",
                         "commit", "build", "deploy", "api", "server", "container", "script", "sdk",
                         "test", "debug", "package", "plugin", "extension", "console", "command",
                         "variable", "function", "module", "version", "release", "pipeline", "azure",
                         "devops", "powershell", "python", "java", "javascript", "xml", "json", "html",
                         "css"]),

        ("Devices", ["phone", "cellphone", "tablet", "laptop", "device", "tv", "printer", "hardware",
                     "screen", "monitor", "keyboard", "mouse", "usb", "bluetooth", "wifi", "drive",
                     "cpu", "memory", "battery", "headset", "speaker", "webcam", "plug", "power",
                     "robot", "sensor", "disk", "chip", "router", "dock", "projector", "remote",
                     "watch", "headphones"]),

        ("Files", ["file", "folder", "document", "page", "pdf", "word", "excel", "powerpoint", "onenote",
                   "zip", "attach", "attachment", "archive", "doc", "txt", "csv", "template", "library",
                   "note", "notebook", "form", "sheet", "slide", "presentation", "storage", "backup"]),

        ("Maps & places", ["map", "location", "globe", "world", "compass", "car", "plane", "airplane",
                           "train", "bus", "flight", "route", "direction", "city", "street", "road",
                           "walk", "bike", "ferry", "taxi", "hotel", "luggage", "passport", "home",
                           "building", "office", "factory", "warehouse", "earth", "region", "country",
                           "place", "geo"]),

        ("Media", ["play", "pause", "stop", "record", "volume", "mute", "music", "video", "movie",
                   "camera", "photo", "picture", "mic", "microphone", "playback", "media", "audio",
                   "sound", "film", "streaming", "subtitle", "playlist", "rewind", "album", "gallery",
                   "image", "podcast", "radio", "live", "cast", "mixer", "equalizer"]),

        ("People", ["people", "person", "contact", "account", "profile", "user", "friend", "group",
                    "team", "follow", "member", "party", "family", "guest", "staff", "employee",
                    "customer", "partner", "community", "crowd", "face", "avatar", "persona",
                    "identity"]),

        ("Security", ["lock", "unlock", "shield", "key", "permission", "certificate", "secure",
                      "security", "password", "privacy", "encryption", "block", "blocked", "protect",
                      "protection", "authenticator", "fingerprint", "verified", "trust", "admin",
                      "policy", "compliance", "vault", "defender", "firewall", "virus", "threat"]),

        ("Status", ["error", "warning", "info", "alert", "success", "completed", "help", "question",
                    "unknown", "status", "badge", "pending", "busy", "away", "available", "critical",
                    "issue", "incident", "checkmark", "cancel", "blocked", "progress", "loading",
                    "verified", "important", "flag", "priority"]),

        ("Text", ["font", "text", "bold", "italic", "underline", "strikethrough", "align", "alignment",
                  "paragraph", "bullet", "bulleted", "numbered", "indent", "style", "format", "header",
                  "heading", "quote", "list", "spelling", "grammar", "character", "letter", "caption",
                  "subscript", "superscript", "highlight", "insert", "title"]),

        ("Time", ["clock", "calendar", "date", "time", "timer", "schedule", "history", "recent", "alarm",
                  "stopwatch", "event", "deadline", "duration", "week", "month", "year", "day", "today",
                  "agenda", "reminder", "snooze", "appointment", "meeting", "hour", "minute"]),

        ("Weather", ["weather", "sunny", "cloud", "cloudy", "rain", "rainy", "snow", "snowy", "storm",
                     "thunder", "wind", "windy", "temperature", "night", "precipitation", "fog",
                     "blowing", "duststorm", "hail", "partly", "frigid", "sunrise", "sunset", "tree",
                     "flower", "leaf", "drizzle", "shower", "humidity", "moon", "sun", "lightning",
                     "tornado", "hurricane", "wave", "mountain", "water", "fire"]),
    ];

    /// <summary>
    /// The words a reader is likely to type that MDL2 does not use, pointing at the name fragments
    /// that answer them. A search for "trash" finding nothing is a search that has failed, not a
    /// set that lacks a delete icon.
    /// <para>
    /// Alias hits rank below every direct match, so an alias can only ever add results to the
    /// bottom of a list - never displace what the reader literally typed.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string[]> aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["trash"] = ["delete", "recyclebin"],
        ["bin"] = ["recyclebin", "delete"],
        ["gear"] = ["settings"],
        ["cog"] = ["settings"],
        ["pencil"] = ["edit"],
        ["house"] = ["home"],
        ["magnifier"] = ["search", "zoom"],
        ["magnify"] = ["search", "zoom"],
        ["user"] = ["contact", "people", "account", "person"],
        ["avatar"] = ["contact", "people", "persona"],
        ["basket"] = ["shoppingcart", "cart"],
        ["logout"] = ["signout"],
        ["login"] = ["signin"],
        ["close"] = ["cancel", "chromeclose", "clear"],
        ["tick"] = ["accept", "checkmark", "completed"],
        ["bell"] = ["ringer", "alert"],
        ["image"] = ["picture", "photo", "image"],
        ["hamburger"] = ["globalnavbutton", "collapsemenu"],
        ["kebab"] = ["more"],
        ["ellipsis"] = ["more"],
        ["dots"] = ["more"],
        ["favourite"] = ["favorite"],
        ["spinner"] = ["progressring", "sync"],
        ["reload"] = ["refresh", "sync"],
        ["eye"] = ["view", "redeye", "hide"],
        ["envelope"] = ["mail"],
        ["plus"] = ["add", "calculatoraddition"],
        ["minus"] = ["remove", "calculatorsubtract"],
        ["cross"] = ["cancel", "chromeclose"],
        ["dark"] = ["clearnight"],
        ["light"] = ["sunny", "brightness"],
        ["language"] = ["localelanguage", "translate"],
        ["terminal"] = ["commandprompt", "developertools"],
        ["trashcan"] = ["delete", "recyclebin"],
        ["garbage"] = ["delete", "recyclebin"],
        ["profile"] = ["persona", "contact", "account"],
        ["menu"] = ["globalnavbutton", "collapsemenu", "more"],
        ["dropdown"] = ["chevrondown"],
        ["success"] = ["completed", "accept", "checkmark"],
        ["danger"] = ["warning", "error", "blocked"],
        ["question"] = ["help", "unknown"],
        ["notification"] = ["ringer", "alert"],
        ["loading"] = ["progressring", "sync"],
        ["moon"] = ["clearnight"],
        ["wrench"] = ["repair", "toolbox"],
        ["tool"] = ["repair", "toolbox", "developertools"],
        ["idea"] = ["lightbulb"],
        ["bulb"] = ["lightbulb"],
        ["exit"] = ["signout", "leave"],
        ["radio"] = ["radiobtn", "radiobullet"],
        ["toggle"] = ["switch"],
        ["battery"] = ["power"],
    };

    /// <summary>
    /// The alias table, for the other readers of this catalog - the MCP server searches the same
    /// set and a word taught here has to answer there too, or the site and the tool disagree about
    /// what the library contains.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> Aliases => aliases;


    /// <summary>Every icon, in the alphabetical order the grid reads in when nothing is typed.</summary>
    public static IReadOnlyList<IconEntry> Items { get; }

    /// <summary>The category names, in chip order.</summary>
    public static IReadOnlyList<string> Categories { get; }

    /// <summary>
    /// The glyphs that need no extra package - shown on the install section, where the reader is
    /// deciding whether they need one.
    /// </summary>
    public static IReadOnlyList<IconEntry> CoreItems { get; }


    static IconCatalog()
    {
        Categories = [.. categoryDefinitions.Select(c => c.Name)];

        // Plurals are folded in here rather than tested for on every one of the ~40,000 word checks
        // below, so a category can be stated in the singular and still catch "Charts" and "People".
        var keywordSets = categoryDefinitions
            .Select(c =>
            {
                var set = new HashSet<string>(StringComparer.Ordinal);

                foreach (var keyword in c.Keywords)
                {
                    set.Add(keyword);
                    set.Add(keyword + "s");
                }

                return set;
            })
            .ToArray();

        var items = new List<IconEntry>(2400);

        foreach (var field in typeof(BitIconName).GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            var name = field.GetValue(null)?.ToString();

            if (string.IsNullOrEmpty(name)) continue;

            var words = SplitWords(name);
            var mask = 0;

            for (var c = 0; c < keywordSets.Length; c++)
            {
                foreach (var word in words)
                {
                    if (keywordSets[c].Contains(word) is false) continue;

                    mask |= 1 << c;
                    break;
                }
            }

            items.Add(new IconEntry(field.Name, name)
            {
                Words = words,
                CategoryMask = mask,
                Lower = name.ToLowerInvariant(),
                InCorePackage = coreGlyphs.Contains(name),
            });
        }

        items.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        Items = items;
        CoreItems = [.. items.Where(i => i.InCorePackage)];
    }


    /// <summary>
    /// The icons a term finds, best first. An empty term is not a search - it is the whole set in
    /// alphabetical order, which is what lets the grid read as a sheet of glyphs.
    /// </summary>
    public static IReadOnlyList<IconEntry> Search(string? term)
    {
        var trimmed = term?.Trim();

        if (string.IsNullOrEmpty(trimmed)) return Items;

        // Spaces are dropped rather than split on, so that "add friend" finds AddFriend: a reader
        // typing what they see in a design has no way of knowing the name is one word.
        var needle = trimmed.Replace(" ", string.Empty).ToLowerInvariant();

        if (needle.Length == 0) return Items;

        var alias = aliases.GetValueOrDefault(needle);

        var matches = new List<(int Rank, int Length, IconEntry Icon)>();

        foreach (var icon in Items)
        {
            var rank = Rank(icon, needle, alias);

            if (rank < 0) continue;

            matches.Add((rank, icon.Name.Length, icon));
        }

        // Rank, then the shorter name: on "mail" the reader means Mail, not MailAlertPrimary, and
        // length is what separates those two once they have tied on rank.
        matches.Sort(static (a, b) =>
        {
            var byRank = a.Rank.CompareTo(b.Rank);
            if (byRank != 0) return byRank;

            var byLength = a.Length.CompareTo(b.Length);
            if (byLength != 0) return byLength;

            return string.Compare(a.Icon.Name, b.Icon.Name, StringComparison.OrdinalIgnoreCase);
        });

        return [.. matches.Select(m => m.Icon)];
    }

    /// <summary>The icons of <paramref name="items"/> in a category, or all of them for null.</summary>
    public static IReadOnlyList<IconEntry> InCategory(IReadOnlyList<IconEntry> items, string? category)
    {
        var index = IndexOf(category);

        if (index < 0) return items;

        var bit = 1 << index;

        return [.. items.Where(i => (i.CategoryMask & bit) != 0)];
    }

    /// <summary>
    /// The categories an icon falls in, in chip order - none for the few the keywords do not
    /// reach. The page reads the mask the other way round, a category at a time; a caller holding
    /// one icon wants the names.
    /// </summary>
    public static IReadOnlyList<string> CategoriesOf(IconEntry icon)
    {
        if (icon.CategoryMask == 0) return [];

        var names = new List<string>(2);

        for (var c = 0; c < Categories.Count; c++)
        {
            if ((icon.CategoryMask & (1 << c)) != 0) names.Add(Categories[c]);
        }

        return names;
    }

    /// <summary>
    /// How many of <paramref name="items"/> fall in each category, in <see cref="Categories"/>
    /// order. It is what turns the chip row into a table of contents rather than a row of guesses:
    /// with a term typed, the counts become that term's own.
    /// </summary>
    public static int[] CountByCategory(IReadOnlyList<IconEntry> items)
    {
        var counts = new int[Categories.Count];

        foreach (var icon in items)
        {
            if (icon.CategoryMask == 0) continue;

            for (var c = 0; c < counts.Length; c++)
            {
                if ((icon.CategoryMask & (1 << c)) != 0) counts[c]++;
            }
        }

        return counts;
    }

    /// <summary>
    /// The other icons built on the same word - Mail beside MailAlert, MailForward, MailReply. An
    /// icon set is chosen in families, and the one a reader wants is often a variant of the one
    /// they searched for.
    /// </summary>
    public static IReadOnlyList<IconEntry> Related(IconEntry icon, int max)
    {
        if (icon.Words.Length == 0) return [];

        // A leading number ("12PointStar") says nothing about what the icon is, so the stem moves
        // on to the word that does.
        var stem = icon.Words[0];

        if (stem.Length < 3 && icon.Words.Length > 1)
        {
            stem = icon.Words[1];
        }

        return [.. Items.Where(i => i.Name != icon.Name && Array.IndexOf(i.Words, stem) >= 0)
                        .OrderBy(i => i.Name.Length)
                        .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                        .Take(max)];
    }

    /// <summary>The icon a name refers to, case-insensitively, or null for one the set does not have.</summary>
    public static IconEntry? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var trimmed = name.Trim();

        return Items.FirstOrDefault(i => string.Equals(i.Name, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The category a query value names, or null for one no chip answers to.</summary>
    public static string? ResolveCategory(string? category)
    {
        var index = IndexOf(category);

        return index < 0 ? null : Categories[index];
    }


    private static int IndexOf(string? category)
    {
        if (string.IsNullOrWhiteSpace(category)) return -1;

        var trimmed = category.Trim();

        for (var c = 0; c < Categories.Count; c++)
        {
            if (string.Equals(Categories[c], trimmed, StringComparison.OrdinalIgnoreCase)) return c;
        }

        return -1;
    }

    /// <summary>
    /// How well an icon answers a term, lowest first, or -1 for one it does not answer at all. The
    /// tiers are the ways a name can be right, strongest first: it IS the term, it opens with it,
    /// one of its words opens with it, an alias says it means it - and last, it merely contains it
    /// somewhere.
    /// <para>
    /// A bare substring ranks below the alias on purpose. It is the weakest signal in the list and
    /// the one most often accidental: "gear" is inside <c>PageArrowRight</c> and
    /// <c>KnowledgeArticle</c>, and neither of them is what the reader meant by it - Settings is.
    /// </para>
    /// </summary>
    private static int Rank(IconEntry icon, string needle, string[]? alias)
    {
        if (icon.Lower == needle) return 0;

        if (icon.Lower.StartsWith(needle, StringComparison.Ordinal)) return 1;

        foreach (var word in icon.Words)
        {
            if (word.StartsWith(needle, StringComparison.Ordinal)) return 2;
        }

        if (alias is not null)
        {
            foreach (var fragment in alias)
            {
                if (icon.Lower.Contains(fragment, StringComparison.Ordinal)) return 3;
            }
        }

        if (icon.Lower.Contains(needle, StringComparison.Ordinal)) return 4;

        return -1;
    }

    /// <summary>
    /// Breaks a PascalCase icon name into lower-case words: "AddFriend" into add + friend,
    /// "AADLogo" into aad + logo, "12PointStar" into 12 + point + star. A run of capitals is one
    /// word up to the last of them, which belongs to the word that follows it.
    /// </summary>
    private static string[] SplitWords(string name)
    {
        var words = new List<string>(4);
        var start = 0;

        for (var i = 1; i <= name.Length; i++)
        {
            var boundary = i == name.Length
                || (char.IsUpper(name[i]) && char.IsUpper(name[i - 1]) is false)
                || (char.IsDigit(name[i]) != char.IsDigit(name[i - 1]))
                || (char.IsUpper(name[i]) && char.IsUpper(name[i - 1]) && i + 1 < name.Length && char.IsLower(name[i + 1]));

            if (boundary is false) continue;

            if (i > start)
            {
                words.Add(name[start..i].ToLowerInvariant());
            }

            start = i;
        }

        return [.. words];
    }
}
