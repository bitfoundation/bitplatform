using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Notifications;

/// <summary>
/// BitPersona in a real browser (/e2e/persona): it prerenders, fills its coin with a picture, the initials (two
/// words, three, an aside, an address, a phone, ImageInitials stepped down to fit), an icon, the unknown glyph or a
/// template, and falls back to the initials when the picture 404s; draws every presence dot in its color with its
/// name, title and glyphs (default, per status, single, external, hidden on small coins); lays every size out with
/// the rows that fit, CoinSize, HidePersonaDetails, Reversed, Vertical and FullWidth with its overflow tooltips;
/// rings, shadows and pushes back a coin; is a real button (overlay, action button revealed on hover and focus) or a
/// real link (rel, target, veil), and while disabled drops the page's activation handlers and its href; paints every
/// coin color and variant (AutoCoinColor hashed, seeded and paletted), re-skins through its public CSS variables and
/// the BitParams cascade, mirrors in RTL with the initials in reading order, and unmounts without an error.
/// </summary>
[TestClass]
public class BitPersonaE2ETests : E2ETestBase
{
    private const string Transparent = "rgba(0, 0, 0, 0)";

    private static readonly (string Name, string Class, string Label, string? DefaultIcon)[] Presences =
    [
        ("Offline", "bit-prs-off", "Offline", "Cancel"),
        ("Online", "bit-prs-onl", "Online", "Accept"),
        ("Away", "bit-prs-awy", "Away", "Clock"),
        ("Dnd", "bit-prs-dnd", "Do not disturb", null),
        ("Blocked", "bit-prs-blk", "Blocked", "Blocked2"),
        ("Busy", "bit-prs-bsy", "Busy", null),
        ("OutOfOffice", "bit-prs-oof", "Out of office", "Airplane"),
        ("Unknown", "bit-prs-unk", "Presence unknown", "Help"),
    ];

    private static readonly (string Name, int Px)[] Sizes =
    [
        ("Size8", 8), ("Size24", 24), ("Size32", 32), ("Size40", 40), ("Size48", 48),
        ("Size56", 56), ("Size72", 72), ("Size100", 100), ("Size120", 120),
    ];

    private static readonly Dictionary<string, string> CoinTextTokens = new()
    {
        ["pri"] = "--bit-clr-pri-text", ["sec"] = "--bit-clr-sec-text", ["ter"] = "--bit-clr-ter-text", ["inf"] = "--bit-clr-inf-text",
        ["suc"] = "--bit-clr-suc-text", ["wrn"] = "--bit-clr-wrn-text", ["swr"] = "--bit-clr-swr-text", ["err"] = "--bit-clr-err-text",
        ["pbg"] = "--bit-clr-fg-pri", ["sbg"] = "--bit-clr-fg-pri", ["tbg"] = "--bit-clr-fg-pri",
        ["pfg"] = "--bit-clr-bg-pri", ["sfg"] = "--bit-clr-bg-pri", ["tfg"] = "--bit-clr-bg-pri",
        ["pbr"] = "--bit-clr-fg-pri", ["sbr"] = "--bit-clr-bg-pri", ["tbr"] = "--bit-clr-bg-pri",
    };

    private static Regex Any => new(".*");

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Coin(string id) => Page.Locator($"#{id} .bit-prs-cne").First;

    private ILocator Presence(string id) => Page.Locator($"#{id} .bit-prs-pre").First;

    // The djb2 hash BitPersona picks an automatic coin color with (GetAutoCoinColorClass).
    private static string AutoColorSuffix(string text, string[] palette)
    {
        uint hash = 5381;
        foreach (var c in text.Trim())
        {
            hash = ((hash << 5) + hash) + c;
        }

        return palette[(int)(hash % (uint)palette.Length)];
    }

    // Where each letter of the initials is painted, left to right on the screen.
    private async Task<string> VisualOrderAsync(string selector)
    {
        return await Page.Locator(selector).EvaluateAsync<string>(@"el => {
            const text = el.firstChild;
            const range = document.createRange();
            const letters = [];
            for (let i = 0; i < text.length; i++) {
                range.setStart(text, i);
                range.setEnd(text, i + 1);
                letters.push({ c: text.data[i], x: range.getBoundingClientRect().left });
            }
            return letters.sort((a, b) => a.x - b.x).map(l => l.c).join('');
        }");
    }

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsThePersonaAndItsInitials()
    {
        var html = await Page.PrerenderedHtmlAsync($"{BaseUrl}/e2e/persona");

        StringAssert.Contains(html, "id=\"basic-initials\"");
        StringAssert.Contains(html, ">SK</span>");

        // A picture that has not loaded yet is backed by the initials while ShowInitialsUntilImageLoads asks for them.
        var untilLoads = html[html.IndexOf("id=\"until-loads-ok\"", StringComparison.Ordinal)..];
        StringAssert.Contains(untilLoads[..untilLoads.IndexOf("</section>", StringComparison.Ordinal)], "bit-prs-ini");

        await OpenAsync("/e2e/persona");
        await Expect(Page.Locator("#basic-initials .bit-prs-ini")).ToHaveTextAsync("SK");
    }

    [TestMethod]
    public async Task Basic_TheCoin_ShowsThePictureElseTheInitialsElseAPersonIcon()
    {
        await OpenAsync("/e2e/persona");

        var image = Page.Locator("#basic-image img.bit-prs-img");
        await Expect(image).ToHaveAttributeAsync("src", "samples/persona-sample.png");
        await Expect(image).ToHaveAttributeAsync("alt", "");
        await Expect(image).ToHaveAttributeAsync("width", "72");
        await Expect(image).ToHaveAttributeAsync("height", "72");
        await Expect(Page.Locator("#basic-loads")).ToHaveTextAsync("1");
        Assert.IsTrue(await image.EvaluateAsync<bool>("img => img.complete && img.naturalWidth === 96"), "The picture loaded.");
        await Expect(Page.Locator("#basic-image .bit-prs-ini")).ToHaveCountAsync(0);
        await Expect(Root("basic-image")).ToHaveClassAsync(HasClass("bit-prs-him"));

        await Expect(Page.Locator("#basic-initials .bit-prs-ini")).ToHaveTextAsync("SK");
        await Expect(Page.Locator("#basic-initials .bit-prs-ini")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Page.Locator("#basic-initials .bit-prs-ptx")).ToHaveTextAsync("Saleh Khafan");
        await Expect(Page.Locator("#basic-initials .bit-prs-stx")).ToHaveTextAsync("Developer");

        // A name with no letters gives no initials: the coin shows a person instead of being left blank.
        await Expect(Page.Locator("#basic-phone .bit-prs-cic")).ToHaveClassAsync(HasClass("bit-icon--Contact"));
        await Expect(Page.Locator("#basic-empty .bit-prs-cic")).ToHaveClassAsync(HasClass("bit-icon--Contact"));
        await Expect(Page.Locator("#basic-empty .bit-prs-det")).ToHaveCountAsync(0);

        // The coin keeps its size in the row.
        await Expect(Coin("basic-initials")).ToHaveCSSAsync("width", "72px");
        await Expect(Coin("basic-initials")).ToHaveCSSAsync("height", "72px");
    }

    [TestMethod]
    public async Task Presence_EachStatus_DrawsItsDotInItsColor_NamedForScreenReaders()
    {
        await OpenAsync("/e2e/persona");

        var colors = new Dictionary<string, string>
        {
            ["Online"] = "--bit-clr-suc", ["Away"] = "--bit-clr-wrn", ["Dnd"] = "--bit-clr-err", ["Busy"] = "--bit-clr-err",
            ["OutOfOffice"] = "--bit-clr-swr", ["Unknown"] = "--bit-clr-fg-ter",
        };

        foreach (var (name, @class, label, _) in Presences)
        {
            var dot = Presence($"pre-{name}");
            await Expect(dot).ToHaveClassAsync(HasClass(@class));
            await Expect(dot).ToHaveAttributeAsync("role", "img");
            await Expect(dot).ToHaveAttributeAsync("aria-label", label);
            await Expect(dot).Not.ToHaveAttributeAsync("title", Any);
            await Expect(dot.Locator("i")).ToHaveCountAsync(0);

            if (colors.TryGetValue(name, out var token))
            {
                await Expect(dot).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync(token));
            }
        }

        // The hollow dots are a ring around the surface.
        await Expect(Presence("pre-Offline")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-bg-pri"));
        await Expect(Presence("pre-Blocked")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("--bit-clr-err"));

        // Dnd carries a bar that tells it apart from Busy.
        Assert.AreNotEqual("none", await Page.PseudoStyleAsync("#pre-Dnd .bit-prs-pre", "::after", "content"));
        Assert.AreEqual("none", await Page.PseudoStyleAsync("#pre-Busy .bit-prs-pre", "::after", "content"));

        // The dot sits in the trailing bottom corner of the coin.
        var coin = await Coin("pre-Online").BoxAsync();
        var box = await Presence("pre-Online").BoxAsync();
        Assert.IsTrue(box.CenterX > coin.CenterX + coin.Width / 4 && box.CenterY > coin.CenterY + coin.Height / 4, "The dot is at the bottom end of the coin.");
    }

    [TestMethod]
    public async Task Presence_DefaultIconsToggledAtRuntime_AddAGlyphPerStatus_OnCoinsOf40AndUp()
    {
        await OpenAsync("/e2e/persona");

        await Page.ClickAsync("#btn-default-icons");

        foreach (var (name, _, _, icon) in Presences)
        {
            var glyph = Page.Locator($"#pre-{name} .bit-prs-pre > i");
            if (icon is null)
            {
                await Expect(glyph).ToHaveCountAsync(0);
            }
            else
            {
                await Expect(glyph).ToHaveClassAsync(HasClass($"bit-icon--{icon}"));
                await Expect(glyph).ToHaveAttributeAsync("aria-hidden", "true");
            }
        }

        // A glyph would not fit the dot of a 32px coin.
        await Expect(Page.Locator("#pre-small .bit-prs-pre > i")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-default-icons");
        await Expect(Page.Locator("#pre-Online .bit-prs-pre > i")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Presence_IconNamesTitlesAndARuntimeChange_DressTheDot()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Page.Locator("#pre-icon-names .bit-prs-pre > i")).ToHaveClassAsync(HasClass("bit-icon--SkypeCheck"));
        await Expect(Page.Locator("#pre-icon-name .bit-prs-pre > i")).ToHaveClassAsync(HasClass("bit-icon--Airplane"));

        await Expect(Presence("pre-titles")).ToHaveAttributeAsync("title", "Focusing");
        await Expect(Presence("pre-titles")).ToHaveAttributeAsync("aria-label", "Focusing");
        await Expect(Presence("pre-title")).ToHaveAttributeAsync("title", "Back at 3");
        await Expect(Presence("pre-title")).ToHaveAttributeAsync("aria-label", "Back at 3");

        // CoinSize-free, the 100px dot is larger than the 56px one.
        Assert.IsTrue((await Presence("pre-titles").BoxAsync()).Width > (await Presence("pre-Online").BoxAsync()).Width);

        await Expect(Presence("pre-runtime")).ToHaveClassAsync(HasClass("bit-prs-onl"));
        await Page.ClickAsync("#btn-presence-busy");
        await Expect(Presence("pre-runtime")).ToHaveClassAsync(HasClass("bit-prs-bsy"));
        await Expect(Presence("pre-runtime")).ToHaveAttributeAsync("aria-label", "Busy");
    }

    [TestMethod]
    public async Task Initials_AreTakenFromTheNameTheWayTheNameAsks()
    {
        await OpenAsync("/e2e/persona");

        var expected = new (string Id, string Initials)[]
        {
            ("ini-two", "SK"), ("ini-three", "TR"), ("ini-aside", "EA"), ("ini-email", "CS"), ("ini-phone", "17"),
            ("ini-image-initials", "SK!"), ("ini-four", "SKHN"), ("ini-no-name", "AB"),
        };

        // What is painted, which the uppercase transform decides (an address yields its letters as written).
        foreach (var (id, initials) in expected)
        {
            await Expect(Page.Locator($"#{id} .bit-prs-ini")).ToHaveTextAsync(initials, new() { UseInnerText = true });
        }

        Assert.AreEqual("uppercase", await ComputedStyleAsync("#ini-two .bit-prs-ini", "text-transform"));
        Assert.AreEqual("none", await ComputedStyleAsync("#ini-two .bit-prs-ini", "user-select"));

        // Initials longer than two letters are stepped down to fit the coin.
        await Expect(Page.Locator("#ini-image-initials .bit-prs-ini")).ToHaveClassAsync(HasClass("bit-prs-in3"));
        await Expect(Page.Locator("#ini-four .bit-prs-ini")).ToHaveClassAsync(HasClass("bit-prs-in4"));
        var two = NotificationsE2EHelpers.Px(await ComputedStyleAsync("#ini-two .bit-prs-ini", "font-size"));
        var three = NotificationsE2EHelpers.Px(await ComputedStyleAsync("#ini-image-initials .bit-prs-ini", "font-size"));
        var four = NotificationsE2EHelpers.Px(await ComputedStyleAsync("#ini-four .bit-prs-ini", "font-size"));
        Assert.IsTrue(two > three && three > four, $"{two} > {three} > {four}");
        var coin = await Coin("ini-four").BoxAsync();
        var letters = await Page.Locator("#ini-four .bit-prs-ini").BoxAsync();
        Assert.IsTrue(letters.Width <= coin.Width, "Four letters fit inside the coin.");

        // Without a name beside them the initials are all a reader has, so they stay in the accessibility tree.
        await Expect(Page.Locator("#ini-no-name .bit-prs-ini")).Not.ToHaveAttributeAsync("aria-hidden", Any);
    }

    [TestMethod]
    public async Task IconAndUnknown_ReplaceTheInitials_AndUnknownCoversAPicture()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Page.Locator("#icon-group .bit-prs-cic")).ToHaveClassAsync(HasClass("bit-icon--Group"));
        await Expect(Page.Locator("#icon-group .bit-prs-ini")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#unknown .bit-prs-cic")).ToHaveClassAsync(HasClass("bit-icon--Help"));
        await Expect(Page.Locator("#unknown img")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#unknown-custom .bit-prs-cic")).ToHaveClassAsync(HasClass("bit-icon--StatusErrorFull"));
    }

    [TestMethod]
    public async Task ShapeAndVariant_SetTheCornerTheDotFollowsAndHowTheColorIsSpent()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Coin("shape-default")).ToHaveCSSAsync("border-top-left-radius", "50%");
        await Expect(Root("shape-pill")).ToHaveClassAsync(HasClass("bit-prs-cir"));
        await Expect(Coin("shape-pill")).ToHaveCSSAsync("border-top-left-radius", "50%");

        await Expect(Root("shape-rounded")).ToHaveClassAsync(HasClass("bit-prs-sqr"));
        var rounded = NotificationsE2EHelpers.Px(await ComputedStyleAsync("#shape-rounded .bit-prs-cne", "border-top-left-radius"));
        Assert.IsTrue(rounded is > 0 and < 36, $"A rounded coin takes the control corner ({rounded}).");
        await Expect(Root("shape-squared")).ToHaveClassAsync(HasClass("bit-prs-sqr"));
        await Expect(Coin("shape-squared")).ToHaveCSSAsync("border-top-left-radius", $"{rounded.ToString(CultureInfo.InvariantCulture)}px");

        await Expect(Root("shape-square")).ToHaveClassAsync(HasClass("bit-prs-sqs"));
        await Expect(Coin("shape-square")).ToHaveCSSAsync("border-top-left-radius", "0px");

        // The dot follows the square corner out.
        var circleCoin = await Coin("shape-default").BoxAsync();
        var circleDot = await Presence("shape-default").BoxAsync();
        var squareCoin = await Coin("shape-rounded").BoxAsync();
        var squareDot = await Presence("shape-rounded").BoxAsync();
        Assert.IsTrue(squareDot.Right - squareCoin.Right > circleDot.Right - circleCoin.Right, "The dot hangs further out of a square coin.");

        var info = await Page.ResolveColorAsync("--bit-clr-inf");
        await Expect(Coin("var-fill")).ToHaveCSSAsync("background-color", info);
        await Expect(Coin("var-fill")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("--bit-clr-inf-text"));
        await Expect(Coin("var-outline")).ToHaveCSSAsync("background-color", Transparent);
        await Expect(Coin("var-outline")).ToHaveCSSAsync("border-top-color", info);
        await Expect(Coin("var-outline")).ToHaveCSSAsync("color", info);
        await Expect(Coin("var-text")).ToHaveCSSAsync("background-color", Transparent);
        await Expect(Coin("var-text")).ToHaveCSSAsync("border-top-color", Transparent);
        await Expect(Coin("var-text")).ToHaveCSSAsync("color", info);
    }

    [TestMethod]
    public async Task Active_RingShadowAndInactive_SetTheCoinApart_AndChangeAtRuntime()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Coin("act-none")).ToHaveCSSAsync("box-shadow", "none");

        await Expect(Root("act-ring")).ToHaveClassAsync(HasClass("bit-prs-arg"));
        var ring = await ComputedStyleAsync("#act-ring .bit-prs-cne", "box-shadow");
        StringAssert.StartsWith(ring, await Page.ResolveColorAsync("--bit-clr-bg-pri"), "The gap is cut in the surface color.");
        StringAssert.Contains(ring, $", {await Page.ResolveColorAsync("--bit-clr-inf")}", "The ring is in the coin color.");

        await Expect(Root("act-shadow")).ToHaveClassAsync(HasClass("bit-prs-ash"));
        var shadow = await ComputedStyleAsync("#act-shadow .bit-prs-cne", "box-shadow");
        Assert.AreNotEqual("none", shadow);
        Assert.IsFalse(shadow.Contains(await Page.ResolveColorAsync("--bit-clr-inf")), "A shadow alone draws no ring.");

        await Expect(Root("act-ringshadow")).ToHaveClassAsync(HasClass("bit-prs-ars"));
        var ringShadow = await ComputedStyleAsync("#act-ringshadow .bit-prs-cne", "box-shadow");
        Assert.IsTrue(ringShadow.Length > ring.Length && ringShadow.StartsWith(ring[..10], StringComparison.Ordinal), "RingShadow draws the ring and the shadow.");

        await Expect(Root("act-inactive")).ToHaveClassAsync(HasClass("bit-prs-ina"));
        await Expect(Page.Locator("#act-inactive .bit-prs-cin")).ToHaveCSSAsync("opacity", "0.8");
        await Expect(Page.Locator("#act-inactive .bit-prs-cin")).ToHaveCSSAsync("transform", "matrix(0.875, 0, 0, 0.875, 0, 0)");

        await Expect(Page.Locator("#act-runtime .bit-prs-cin")).ToHaveCSSAsync("opacity", "0.8");
        await Page.ClickAsync("#btn-activate");
        await Expect(Root("act-runtime")).ToHaveClassAsync(HasClass("bit-prs-arg"));
        await Expect(Root("act-runtime")).Not.ToHaveClassAsync(HasClass("bit-prs-ina"));
        await Expect(Page.Locator("#act-runtime .bit-prs-cin")).ToHaveCSSAsync("opacity", "1");
    }

    [TestMethod]
    public async Task Action_TheButton_IsRevealedOnHoverAndFocus_AndFiresOnce()
    {
        await OpenAsync("/e2e/persona");

        var action = Page.Locator("#action-btn .bit-prs-abt");
        await Expect(action).ToHaveAttributeAsync("title", "Edit image");
        await Expect(action).ToHaveAttributeAsync("aria-label", "Edit image");
        await Expect(action).ToHaveAttributeAsync("type", "button");
        await Expect(Page.Locator("#action-btn .bit-prs-aic")).ToHaveClassAsync(HasClass("bit-icon--CloudUpload"));
        await Expect(action).ToHaveCSSAsync("opacity", "0");

        await Coin("action-btn").HoverAsync();
        await Expect(action).ToHaveCSSAsync("opacity", "1");

        await action.ClickAsync();
        await Expect(Page.Locator("#action-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#action-last-button")).ToHaveTextAsync("0");

        // The keyboard reveals it too.
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(Page.Locator("#action-title .bit-prs-abt")).ToHaveCSSAsync("opacity", "0");
        await action.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        var title = Page.Locator("#action-title .bit-prs-abt");
        await Expect(title).ToBeFocusedAsync();
        await Expect(title).ToHaveCSSAsync("opacity", "1");
        await Expect(title).ToHaveAttributeAsync("title", "Upload photo");
        Assert.AreNotEqual("none", await ComputedStyleAsync("#action-title .bit-prs-abt", "box-shadow"));

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#action-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Action_ATemplate_ReplacesTheButton_AndACoinOnlyPersonaNamesItsCoin()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Page.Locator("#action-template .bit-prs-abt")).ToHaveCountAsync(0);
        await Page.Locator("#tpl-action").ClickAsync(new() { Force = true });
        await Expect(Page.Locator("#template-action-clicks")).ToHaveTextAsync("1");

        await Expect(Page.Locator("#action-coin-only .bit-prs-det")).ToHaveCountAsync(0);
        await Expect(Coin("action-coin-only")).ToHaveAttributeAsync("role", "img");
        await Expect(Coin("action-coin-only")).ToHaveAttributeAsync("aria-label", "Xafan Salina");
    }

    [TestMethod]
    public async Task ImageClick_TheCoinIsAButton_RevealingItsOverlay_AndFiresOnce()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Root("image-click")).ToHaveClassAsync(HasClass("bit-prs-iac"));
        var coin = Page.Locator("#image-click button.bit-prs-cne");
        await Expect(coin).ToHaveAttributeAsync("type", "button");
        await Expect(coin).ToHaveAttributeAsync("aria-label", "Change photo");
        Assert.AreEqual("pointer", await ComputedStyleAsync("#image-click .bit-prs-cne", "cursor"));

        var overlay = Page.Locator("#image-click .bit-prs-imo");
        await Expect(overlay).ToHaveTextAsync("Change photo");
        await Expect(overlay).ToHaveCSSAsync("opacity", "0");
        await Expect(overlay).ToHaveCSSAsync("visibility", "hidden");

        await coin.HoverAsync();
        await Expect(overlay).ToHaveCSSAsync("opacity", "1");
        await Expect(overlay).ToHaveCSSAsync("visibility", "visible");

        await coin.ClickAsync();
        await Expect(Page.Locator("#image-clicks")).ToHaveTextAsync("1");

        // A coin of initials is just as clickable and veiled.
        await Page.Locator("#image-click-initials .bit-prs-cne").ClickAsync();
        await Expect(Page.Locator("#image-clicks")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#image-click-initials .bit-prs-imo")).ToHaveTextAsync("Add photo");

        // An empty overlay text leaves the coin unveiled and named after the person.
        await Expect(Page.Locator("#image-click-empty .bit-prs-imo")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#image-click-empty .bit-prs-cne")).ToHaveAttributeAsync("aria-label", "Xafan Salina");

        // The keyboard reaches it, rings it and unveils it; Space activates it.
        await Page.Mouse.MoveAsync(0, 0);
        await Page.Locator("#image-click-initials .bit-prs-cne").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(coin).ToBeFocusedAsync();
        await Expect(overlay).ToHaveCSSAsync("opacity", "1");
        Assert.AreNotEqual("none", await ComputedStyleAsync("#image-click .bit-prs-cne", "box-shadow"));
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#image-clicks")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task Link_TheCoinIsARealLink_WithRelTargetAndAVeilOnRequest()
    {
        await OpenAsync("/e2e/persona");

        var link = Page.Locator("#link-self a.bit-prs-cne");
        await Expect(link).ToHaveAttributeAsync("href", "/e2e/persona#link-target");
        await Expect(link).ToHaveAttributeAsync("aria-label", "Xafan Salina");
        await Expect(link).Not.ToHaveAttributeAsync("rel", Any);
        await Expect(Page.Locator("#link-self .bit-prs-imo")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#link-blank a")).ToHaveAttributeAsync("target", "_blank");
        await Expect(Page.Locator("#link-blank a")).ToHaveAttributeAsync("rel", "noopener");
        await Expect(Page.Locator("#link-rel a")).ToHaveAttributeAsync("rel", "nofollow noopener");

        await Expect(Page.Locator("#link-veil .bit-prs-imo")).ToHaveTextAsync("View profile");
        await Expect(Page.Locator("#link-veil a")).ToHaveAttributeAsync("aria-label", "View profile");
        await Expect(Page.Locator("#link-hidden-details a")).ToHaveAttributeAsync("aria-label", "Xafan Salina, View profile");

        await link.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/e2e/persona#link-target$"));
        await Expect(Root("link-self")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Disabled_DimsTheCoin_DisablesItsControls_AndDropsThePagesActivationHandlers()
    {
        await OpenAsync("/e2e/persona");

        var root = Root("dis-plain");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(root).ToHaveCSSAsync("pointer-events", "none");
        await Expect(Page.Locator("#dis-plain .bit-prs-cin")).Not.ToHaveCSSAsync("opacity", "1");
        await Expect(Page.Locator("#dis-plain .bit-prs-ptx")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("--bit-clr-fg-dis"));
        await Expect(Page.Locator("#dis-plain .bit-prs-stx")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("--bit-clr-fg-dis"));

        // What a screen reader or a script delivers is dropped; a hover is not an activation and stays.
        await root.DispatchEventAsync("click");
        await root.DispatchEventAsync("keydown", new { key = "Enter" });
        await root.DispatchEventAsync("mouseover");
        await Expect(Page.Locator("#root-hovers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#root-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#root-keys")).ToHaveTextAsync("0");

        var coin = Page.Locator("#dis-click button.bit-prs-cne");
        await Expect(coin).ToBeDisabledAsync();
        await Expect(coin).ToHaveAttributeAsync("aria-disabled", "true");
        var action = Page.Locator("#dis-click .bit-prs-abt");
        await Expect(action).ToBeDisabledAsync();
        await Expect(action).ToHaveAttributeAsync("aria-disabled", "true");
        await coin.DispatchEventAsync("click");
        await action.DispatchEventAsync("click");
        await Expect(Page.Locator("#image-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#action-clicks")).ToHaveTextAsync("0");

        var link = Page.Locator("#dis-link a.bit-prs-cne");
        await Expect(link).Not.ToHaveAttributeAsync("href", Any);
        await Expect(link).ToHaveAttributeAsync("role", "link");
        await Expect(link).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(link).ToHaveAttributeAsync("aria-disabled", "true");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await root.DispatchEventAsync("click");
        await root.DispatchEventAsync("keydown", new { key = "Enter" });
        await Expect(Page.Locator("#root-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#root-keys")).ToHaveTextAsync("1");

        await Expect(coin).ToBeEnabledAsync();
        await coin.ClickAsync();
        await Expect(Page.Locator("#image-clicks")).ToHaveTextAsync("1");
        await Expect(link).ToHaveAttributeAsync("href", "#link-target");
        await Expect(link).Not.ToHaveAttributeAsync("aria-disabled", Any);
    }

    [TestMethod]
    public async Task Image_LoadingAttributesAltSourcesFadeAndARuntimeSource_AreAppliedToTheImg()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Page.Locator("#img-lazy img")).ToHaveAttributeAsync("loading", "lazy");
        await Expect(Page.Locator("#img-eager img")).ToHaveAttributeAsync("loading", "eager");
        await Expect(Page.Locator("#img-eager img")).ToHaveAttributeAsync("draggable", "false");
        await Expect(Page.Locator("#img-eager img")).ToHaveAttributeAsync("decoding", "async");

        var alt = Page.Locator("#img-alt img");
        await Expect(alt).ToHaveAttributeAsync("alt", "Xafan smiling");
        await Expect(alt).ToHaveAttributeAsync("sizes", "72px");
        await Expect(alt).ToHaveAttributeAsync("srcset", "samples/persona-sample-72.png 72w, samples/persona-sample.png 96w");
        await Page.WaitForFunctionAsync("() => document.querySelector('#img-alt img').complete");
        StringAssert.Contains(await alt.EvaluateAsync<string>("img => img.currentSrc"), "samples/persona-sample");

        // Candidates alone are a picture too.
        await Expect(Root("img-srcset-only")).ToHaveClassAsync(HasClass("bit-prs-him"));
        await Expect(Page.Locator("#img-srcset-only img")).Not.ToHaveAttributeAsync("src", new Regex(".+"));

        await Page.ClickAsync("#btn-fade");
        var fade = Page.Locator("#img-fade img");
        await Expect(fade).ToHaveClassAsync(HasClass("bit-prs-ifi"));
        await Expect(fade).ToHaveCSSAsync("animation-name", "bit-prs-fade-in-anm");
        await Expect(fade).ToHaveCSSAsync("opacity", "1");

        await Expect(Page.Locator("#img-runtime .bit-prs-ini")).ToHaveTextAsync("SK");
        await Page.ClickAsync("#btn-set-image");
        await Expect(Page.Locator("#runtime-loads")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#img-runtime img")).ToBeVisibleAsync();
        await Expect(Page.Locator("#img-runtime .bit-prs-ini")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ImageError_APictureThat404s_FallsBackToTheInitialsOrTheIcon_AndReportsIt()
    {
        // The browser reports each 404 on the console; those are the subject here.
        AllowConsoleErrors = true;
        await OpenAsync("/e2e/persona");

        await Page.ClickAsync("#btn-broken");

        await Expect(Page.Locator("#image-errors")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#err-plain img")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#err-plain .bit-prs-ini")).ToHaveTextAsync("XS");
        await Expect(Root("err-plain")).ToHaveClassAsync(HasClass("bit-prs-him"));

        await Expect(Page.Locator("#err-until img")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#err-until .bit-prs-ini")).ToHaveTextAsync("SK");

        await Expect(Page.Locator("#err-no-name img")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#err-no-name .bit-prs-cic")).ToHaveClassAsync(HasClass("bit-icon--Contact"));

        Assert.IsTrue(ConsoleErrors.All(e => e.Contains("404") || e.Contains("Failed to load resource", StringComparison.OrdinalIgnoreCase)),
            $"Only the 404s are reported: {string.Join(" | ", ConsoleErrors)}");
    }

    [TestMethod]
    public async Task ShowInitialsUntilImageLoads_TakesTheInitialsAwayOnceThePictureIsIn()
    {
        await OpenAsync("/e2e/persona");

        await Page.WaitForFunctionAsync("() => document.querySelector('#until-loads-ok img')?.complete === true");
        await Expect(Page.Locator("#until-loads-ok .bit-prs-ini")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#until-loads-ok img")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Templates_ReplaceEachTextRowTheOverlayAndTheCoin()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Page.Locator("#tpl .bit-prs-ptx #tpl-primary")).ToHaveTextAsync("Xafan Salina");
        await Expect(Page.Locator("#tpl .bit-prs-stx #tpl-secondary")).ToHaveTextAsync("Software Engineer");
        await Expect(Page.Locator("#tpl .bit-prs-ttx #tpl-tertiary")).ToHaveTextAsync("In a meeting");
        await Expect(Page.Locator("#tpl .bit-prs-otx #tpl-optional")).ToHaveTextAsync("Available at 7");
        await Expect(Page.Locator("#tpl .bit-prs-imo #tpl-overlay")).ToHaveTextAsync("Edit!");

        await Coin("tpl").HoverAsync();
        await Expect(Page.Locator("#tpl .bit-prs-imo")).ToHaveCSSAsync("opacity", "1");

        await Expect(Page.Locator("#tpl-coin .bit-prs-cne #tpl-coin-img")).ToBeVisibleAsync();
        await Expect(Coin("tpl-coin")).Not.ToHaveClassAsync(HasClass("bit-prs-imc"));
        await Expect(Page.Locator("#tpl-coin .bit-prs-ini")).ToHaveCountAsync(0);
        await Expect(Presence("tpl-coin")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task AutoCoinColor_HashesTheSeedTheInitialsOrTheName_IntoThePalette()
    {
        await OpenAsync("/e2e/persona");

        string[] palette = ["pri", "sec", "ter", "suc", "wrn", "err", "inf"];
        foreach (var (id, text) in new[] { ("auto-1", "Xafan Salina"), ("auto-2", "Saleh Khafan"), ("auto-3", "Ted Randall") })
        {
            var suffix = AutoColorSuffix(text, palette);
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-prs-{suffix}"));
            await Expect(Coin(id)).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync(NotificationsE2EHelpers.Colors.First(c => c.Suffix == suffix).Token));
        }

        // The same seed gives the same color whatever the name says.
        var seeded = AutoColorSuffix("u-1024", palette);
        await Expect(Root("auto-seed-1")).ToHaveClassAsync(HasClass($"bit-prs-{seeded}"));
        await Expect(Root("auto-seed-2")).ToHaveClassAsync(HasClass($"bit-prs-{seeded}"));

        string[] own = ["ter", "swr"];
        await Expect(Root("auto-palette-1")).ToHaveClassAsync(HasClass($"bit-prs-{AutoColorSuffix("Carlos Slattery", own)}"));
        await Expect(Root("auto-palette-2")).ToHaveClassAsync(HasClass($"bit-prs-{AutoColorSuffix("Elvia Atkins", own)}"));

        await Expect(Root("auto-explicit")).ToHaveClassAsync(HasClass("bit-prs-suc"));
        await Expect(Root("auto-empty")).ToHaveClassAsync(HasClass("bit-prs-inf"));
    }

    [TestMethod]
    public async Task Layout_ReversedVerticalAndFullWidth_ArrangeTheCoinAndTheTexts()
    {
        await OpenAsync("/e2e/persona");

        var coin = await Coin("lay-default").BoxAsync();
        var details = await Page.Locator("#lay-default .bit-prs-det").BoxAsync();
        Assert.IsTrue(coin.Right <= details.Left, "The coin leads the texts.");
        Assert.IsTrue((await Presence("lay-default").BoxAsync()).CenterX > coin.CenterX, "The dot is at the trailing side.");

        await Expect(Root("lay-reversed")).ToHaveClassAsync(HasClass("bit-prs-rvs"));
        var rCoin = await Coin("lay-reversed").BoxAsync();
        var rDetails = await Page.Locator("#lay-reversed .bit-prs-det").BoxAsync();
        Assert.IsTrue(rCoin.Left >= rDetails.Right, "Reversed puts the coin after the texts.");
        Assert.IsTrue((await Presence("lay-reversed").BoxAsync()).CenterX < rCoin.CenterX, "The dot follows to the leading side.");

        var vCoin = await Coin("lay-vertical").BoxAsync();
        var vDetails = await Page.Locator("#lay-vertical .bit-prs-det").BoxAsync();
        Assert.IsTrue(vCoin.Bottom <= vDetails.Top, "Vertical stacks the coin over the texts.");
        Assert.AreEqual(vCoin.CenterX, vDetails.CenterX, 2, "Both are centred.");
        Assert.AreEqual("center", await ComputedStyleAsync("#lay-vertical .bit-prs-det", "text-align"));

        var vrCoin = await Coin("lay-vertical-reversed").BoxAsync();
        var vrDetails = await Page.Locator("#lay-vertical-reversed .bit-prs-det").BoxAsync();
        Assert.IsTrue(vrDetails.Bottom <= vrCoin.Top, "Vertical and Reversed put the texts over the coin.");

        var box = await Page.BoxAsync("#fw-box");
        var full = await Page.BoxAsync("#lay-full");
        Assert.AreEqual(box.Width - 16 - 2, full.Width, 1, "FullWidth stretches across the container.");
        var primary = Page.Locator("#lay-full .bit-prs-ptx");
        Assert.IsTrue(await primary.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "The long name ellipsizes.");
        Assert.AreEqual("ellipsis", await ComputedStyleAsync("#lay-full .bit-prs-ptx", "text-overflow"));
        await Expect(primary).ToHaveAttributeAsync("title", "Xafan Salina Abdollahzadeh Yusefnejad Longname");
        await Expect(Page.Locator("#lay-full .bit-prs-stx")).ToHaveAttributeAsync("title", "Principal Software Engineer, Developer Experience");

        await Expect(Page.Locator("#lay-no-tooltip .bit-prs-ptx")).Not.ToHaveAttributeAsync("title", Any);
    }

    [TestMethod]
    public async Task Cascade_BitParams_SetsTheDefaultsAndAnOwnValueWins()
    {
        await OpenAsync("/e2e/persona");

        foreach (var id in new[] { "cas-1", "cas-2" })
        {
            await Expect(Root(id)).ToHaveClassAsync(HasClass("bit-prs-s40"));
            await Expect(Root(id)).ToHaveClassAsync(HasClass("bit-prs-sqr"));
            await Expect(Coin(id)).ToHaveCSSAsync("width", "40px");
        }

        string[] palette = ["pri", "sec", "ter", "suc", "wrn", "err", "inf"];
        await Expect(Root("cas-1")).ToHaveClassAsync(HasClass($"bit-prs-{AutoColorSuffix("Xafan Salina", palette)}"));

        await Expect(Presence("cas-1")).ToHaveAttributeAsync("title", "Available");
        await Expect(Presence("cas-1")).ToHaveAttributeAsync("aria-label", "Available");
        await Expect(Page.Locator("#cas-1 .bit-prs-pre > i")).ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(Presence("cas-2")).ToHaveAttributeAsync("aria-label", "Be right back");

        await Expect(Root("cas-3")).ToHaveClassAsync(HasClass("bit-prs-s72"));
        await Expect(Coin("cas-3")).ToHaveCSSAsync("width", "72px");
        await Expect(Presence("cas-3")).ToHaveAttributeAsync("aria-label", "Busy");
    }

    [TestMethod]
    public async Task Color_EachCoinColor_PaintsTheCoinAndItsInitials()
    {
        await OpenAsync("/e2e/persona");

        foreach (var (name, suffix, token) in NotificationsE2EHelpers.Colors)
        {
            var main = await Page.ResolveColorAsync(token);

            await Expect(Root($"clr-{name}")).ToHaveClassAsync(HasClass($"bit-prs-{suffix}"));
            await Expect(Coin($"clr-{name}")).ToHaveCSSAsync("background-color", main);
            await Expect(Coin($"clr-{name}")).ToHaveCSSAsync("color", await Page.ResolveColorAsync(CoinTextTokens[suffix]));

            await Expect(Coin($"clr-otl-{name}")).ToHaveCSSAsync("background-color", Transparent);
            await Expect(Coin($"clr-otl-{name}")).ToHaveCSSAsync("border-top-color", main);
            await Expect(Coin($"clr-otl-{name}")).ToHaveCSSAsync("color", main);
        }
    }

    [TestMethod]
    public async Task ExternalIcons_EveryIconTakesABitIconInfo()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Page.Locator("#ext-action .bit-prs-aic")).ToHaveClassAsync(HasClass("fa-camera"));
        await Expect(Page.Locator("#ext-action .bit-prs-aic")).ToHaveClassAsync(HasClass("fa-solid"));

        var coinIcon = Page.Locator("#ext-coin .bit-prs-cic");
        await Expect(coinIcon).ToHaveClassAsync(HasClass("prs-ext"));
        await Expect(coinIcon).ToHaveClassAsync(HasClass("custom-glyph"));
        Assert.AreEqual("\"*\"", await Page.PseudoStyleAsync("#ext-coin .bit-prs-cic", "::before", "content"));

        await Expect(Page.Locator("#ext-presence .bit-prs-pre > i")).ToHaveClassAsync(HasClass("bi-check-circle-fill"));
        await Expect(Page.Locator("#ext-presence-single .bit-prs-pre > i")).ToHaveClassAsync(HasClass("bi-dash-circle"));
        await Expect(Page.Locator("#ext-unknown .bit-prs-cic")).ToHaveClassAsync(HasClass("fa-user-secret"));
    }

    [TestMethod]
    public async Task Size_EachSize_SizesTheCoinAndShowsTheRowsThatFit()
    {
        await OpenAsync("/e2e/persona");

        foreach (var (name, px) in Sizes)
        {
            var id = $"size-{name}";
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-prs-s{px}"));
            await Expect(Root(id)).ToHaveClassAsync(HasClass("bit-prs-ssz"));

            if (px == 8)
            {
                await Expect(Page.Locator($"#{id} .bit-prs-imc")).ToHaveCountAsync(0);
                await Expect(Presence(id)).ToHaveCSSAsync("position", "static");
                Assert.IsTrue((await Presence(id).BoxAsync()).Right <= (await Page.Locator($"#{id} .bit-prs-det").BoxAsync()).Left, "The dot stands in for the coin in the row.");
            }
            else
            {
                await Expect(Coin(id)).ToHaveCSSAsync("width", $"{px}px");
                await Expect(Page.Locator($"#{id} img, #{id} .bit-prs-ini").First).ToBeVisibleAsync();
            }

            await Expect(Page.Locator($"#{id} .bit-prs-ptx")).ToBeVisibleAsync();
            await (px >= 40 ? Expect(Page.Locator($"#{id} .bit-prs-stx")).ToBeVisibleAsync() : Expect(Page.Locator($"#{id} .bit-prs-stx")).ToBeHiddenAsync());
            await (px >= 72 ? Expect(Page.Locator($"#{id} .bit-prs-ttx")).ToBeVisibleAsync() : Expect(Page.Locator($"#{id} .bit-prs-ttx")).ToBeHiddenAsync());
            await (px >= 100 ? Expect(Page.Locator($"#{id} .bit-prs-otx")).ToBeVisibleAsync() : Expect(Page.Locator($"#{id} .bit-prs-otx")).ToBeHiddenAsync());
        }

        // An unset size is laid out as Size48 and leaves the gap to the public variable.
        await Expect(Root("size-unset")).ToHaveClassAsync(HasClass("bit-prs-s48"));
        await Expect(Root("size-unset")).Not.ToHaveClassAsync(HasClass("bit-prs-ssz"));
        await Expect(Coin("size-unset")).ToHaveCSSAsync("width", "48px");

        await Expect(Page.Locator("#size-show-secondary .bit-prs-stx")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Size_CoinSizeAndHidePersonaDetails_SizeTheCoinAndLeaveItAlone()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Coin("size-coin150")).ToHaveCSSAsync("width", "150px");
        await Expect(Coin("size-coin150")).ToHaveCSSAsync("height", "150px");
        await Expect(Coin("size-coin150")).ToHaveCSSAsync("font-size", "60px");
        await Expect(Presence("size-coin150")).ToHaveCSSAsync("width", "37.5px");
        await Expect(Presence("size-coin150-rounded")).ToHaveAttributeAsync("style", new Regex("--bit-prs-presence-inset:-12.5px"));

        // Coin only: named after the person and the status.
        await Expect(Page.Locator("#size-coin-only .bit-prs-det")).ToHaveCountAsync(0);
        await Expect(Root("size-coin-only")).ToHaveAttributeAsync("role", "img");
        await Expect(Root("size-coin-only")).ToHaveAttributeAsync("aria-label", "Xafan Salina, Online");

        await Page.ClickAsync("#btn-hide-details");
        foreach (var (name, px) in Sizes)
        {
            var details = Page.Locator($"#size-{name} .bit-prs-det");
            await Expect(details).ToHaveCountAsync(px == 8 ? 1 : 0);
        }

        await Expect(Root("size-Size72")).ToHaveAttributeAsync("aria-label", "Xafan Salina, Online");

        await Page.ClickAsync("#btn-hide-details");
        await Expect(Page.Locator("#size-Size72 .bit-prs-det")).ToHaveCountAsync(1);
        await Expect(Root("size-Size72")).Not.ToHaveAttributeAsync("role", Any);
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRootAndStylesAndClassesOnEveryPart()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Root("sc-style")).ToHaveCSSAsync("padding-top", "16px");
        await Expect(Root("sc-style")).ToHaveCSSAsync("background-color", "rgb(128, 128, 128)");
        await Expect(Root("sc-class")).ToHaveClassAsync(HasClass("custom-class"));

        var styles = new (string Selector, string Property, string Value)[]
        {
            ("#sc-styles", "margin-top", "3px"),
            ("#sc-styles .bit-prs-cin", "margin-left", "2px"),
            ("#sc-styles .bit-prs-pre > i", "color", "rgb(0, 0, 255)"),
            ("#sc-styles .bit-prs-cne", "background-color", "rgb(0, 255, 144)"),
            ("#sc-styles .bit-prs-ini", "letter-spacing", "3px"),
            ("#sc-styles .bit-prs-imo", "padding-top", "4px"),
            ("#sc-styles .bit-prs-imo > span", "font-style", "italic"),
            ("#sc-styles .bit-prs-abt", "margin-bottom", "1px"),
            ("#sc-styles .bit-prs-aic", "color", "rgb(255, 0, 0)"),
            ("#sc-styles .bit-prs-pre", "border-top-color", "rgb(182, 255, 0)"),
            ("#sc-styles .bit-prs-det", "padding-left", "5px"),
            ("#sc-styles .bit-prs-ptx", "color", "rgb(234, 25, 25)"),
            ("#sc-styles .bit-prs-stx", "color", "rgb(1, 2, 3)"),
            ("#sc-styles .bit-prs-ttx", "color", "rgb(4, 5, 6)"),
            ("#sc-styles .bit-prs-otx", "color", "rgb(7, 8, 9)"),
            ("#sc-icons .bit-prs-cic", "color", "rgb(10, 20, 30)"),
            ("#sc-unknown .bit-prs-cic", "color", "rgb(40, 50, 60)"),
            ("#sc-image img", "opacity", "0.5"),
            ("#sc-small .bit-prs-pre", "margin-right", "6px"),
        };

        foreach (var (selector, property, value) in styles)
        {
            await Expect(Page.Locator(selector).First).ToHaveCSSAsync(property, value);
        }

        var classes = new (string Selector, string Class)[]
        {
            ("#sc-classes", "c-root"), ("#sc-classes .bit-prs-cin", "c-cin"), ("#sc-classes .bit-prs-pre > i", "c-pic"),
            ("#sc-classes .bit-prs-cne", "c-imc"), ("#sc-classes .bit-prs-ini", "c-ini"), ("#sc-classes .bit-prs-imo", "c-imo"),
            ("#sc-classes .bit-prs-imo > span", "c-imt"), ("#sc-classes .bit-prs-abt", "c-abt"), ("#sc-classes .bit-prs-aic", "c-aic"),
            ("#sc-classes .bit-prs-pre", "c-pre"), ("#sc-classes .bit-prs-det", "c-det"), ("#sc-classes .bit-prs-ptx", "c-ptx"),
            ("#sc-classes .bit-prs-stx", "c-stx"), ("#sc-classes .bit-prs-ttx", "c-ttx"), ("#sc-classes .bit-prs-otx", "c-otx"),
            ("#sc-icons .bit-prs-cic", "c-cic"), ("#sc-unknown .bit-prs-cic", "c-unk"), ("#sc-image img", "c-img"),
            ("#sc-small .bit-prs-pre", "c-prs"),
        };

        foreach (var (selector, @class) in classes)
        {
            await Expect(Page.Locator(selector).First).ToHaveClassAsync(HasClass(@class));
        }
    }

    [TestMethod]
    public async Task Rtl_ExplicitAndInherited_MirrorTheRow_AndKeepLatinInitialsInReadingOrder()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Root("rtl-latin")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("rtl-latin")).ToHaveClassAsync(HasClass("bit-rtl"));

        foreach (var id in new[] { "rtl-latin", "rtl-persian", "rtl-inherited" })
        {
            var coin = await Coin(id).BoxAsync();
            var details = await Page.Locator($"#{id} .bit-prs-det").BoxAsync();
            Assert.IsTrue(coin.Left >= details.Right, $"{id}: the coin is on the right.");
            Assert.IsTrue((await Presence(id).BoxAsync()).CenterX < coin.CenterX, $"{id}: the dot is at the trailing (left) side.");
        }

        // The first initial lands on the right, the way a right-to-left reader starts.
        await Expect(Page.Locator("#rtl-latin .bit-prs-ini")).ToHaveTextAsync("KS");
        Assert.AreEqual("KS", await VisualOrderAsync("#rtl-latin .bit-prs-ini"));

        await Expect(Page.Locator("#rtl-inherited .bit-prs-ini")).ToHaveTextAsync("SK");
        await Expect(Page.Locator("#rtl-inherited .bit-prs-ini")).ToHaveClassAsync(HasClass("bit-prs-rev"));
        Assert.AreEqual("KS", await VisualOrderAsync("#rtl-inherited .bit-prs-ini"), "An inherited RTL lays the pair out from the right.");

        Assert.AreEqual("SK", await VisualOrderAsync("#ltr-latin .bit-prs-ini"));
        await Expect(Page.Locator("#rtl-persian .bit-prs-ini")).ToHaveTextAsync("صن");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheDefault_AndTheParametersWinOverThem()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Coin("cv-coin")).ToHaveCSSAsync("background-color", "rgb(122, 46, 142)");
        await Expect(Coin("cv-coin")).ToHaveCSSAsync("color", "rgb(255, 255, 255)");
        await Expect(Coin("cv-coin")).ToHaveCSSAsync("border-top-left-radius", "12px");
        await Expect(Root("cv-coin")).ToHaveCSSAsync("column-gap", "24px");
        await Expect(Page.Locator("#cv-coin .bit-prs-ptx")).ToHaveCSSAsync("font-weight", "600");
        await Expect(Root("cv-coin")).ToHaveCSSAsync("color", "rgb(10, 10, 10)");
        await Expect(Page.Locator("#cv-coin .bit-prs-stx")).ToHaveCSSAsync("color", "rgb(122, 46, 142)");
        await Expect(Presence("cv-coin")).ToHaveCSSAsync("background-color", "rgb(180, 0, 158)");
        await Expect(Presence("cv-coin")).ToHaveCSSAsync("color", "rgb(255, 255, 254)");
        await Expect(Presence("cv-coin")).ToHaveCSSAsync("border-top-color", "rgb(1, 1, 1)");

        // An explicit CoinColor and Shape win over the variables.
        await Expect(Coin("cv-explicit")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("--bit-clr-err"));
        await Expect(Coin("cv-explicit")).ToHaveCSSAsync("border-top-left-radius", "0px");
        Assert.AreNotEqual("24px", await ComputedStyleAsync("#cv-explicit", "column-gap"), "An explicit Size wins over the gap.");

        var ring = await ComputedStyleAsync("#cv-ring .bit-prs-cne", "box-shadow");
        StringAssert.Contains(ring, "rgb(200, 0, 0) 0px 0px 0px 3px");
        StringAssert.Contains(ring, "rgb(0, 0, 200) 0px 0px 0px 8px");
        await Expect(Coin("cv-shadow")).ToHaveCSSAsync("box-shadow", "rgb(0, 0, 0) 0px 0px 9px 0px");

        await Expect(Page.Locator("#cv-inactive .bit-prs-cin")).ToHaveCSSAsync("opacity", "0.5");
        await Expect(Page.Locator("#cv-inactive .bit-prs-cin")).ToHaveCSSAsync("transform", "matrix(0.5, 0, 0, 0.5, 0, 0)");

        await Expect(Page.Locator("#cv-overlay .bit-prs-imo")).ToHaveCSSAsync("background-color", "rgb(0, 100, 0)");
        await Expect(Page.Locator("#cv-overlay .bit-prs-imo")).ToHaveCSSAsync("color", "rgb(255, 255, 0)");
        var action = Page.Locator("#cv-overlay .bit-prs-abt");
        await Expect(action).ToHaveCSSAsync("background-color", "rgb(50, 50, 50)");
        await Expect(action).ToHaveCSSAsync("color", "rgb(250, 250, 250)");
        await Coin("cv-overlay").HoverAsync();
        await action.HoverAsync();
        await Expect(action).ToHaveCSSAsync("background-color", "rgb(90, 90, 90)");

        await Page.Mouse.MoveAsync(0, 0);
        await action.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Coin("cv-overlay")).ToBeFocusedAsync();
        StringAssert.Contains(await ComputedStyleAsync("#cv-overlay .bit-prs-cne", "box-shadow"), "rgb(255, 0, 255)");

        await Expect(Page.Locator("#cv-disabled .bit-prs-ptx")).ToHaveCSSAsync("color", "rgb(150, 150, 150)");
        await Expect(Page.Locator("#cv-disabled .bit-prs-stx")).ToHaveCSSAsync("color", "rgb(150, 150, 150)");

        await Expect(Presence("cv-surface")).ToHaveCSSAsync("background-color", "rgb(240, 240, 200)");
        StringAssert.StartsWith(await ComputedStyleAsync("#cv-surface .bit-prs-cne", "box-shadow"), "rgb(240, 240, 200)");

        var presenceColors = new (string Name, string Property, string Value)[]
        {
            ("Online", "background-color", "rgb(0, 1, 2)"), ("Away", "background-color", "rgb(0, 1, 3)"),
            ("Busy", "background-color", "rgb(0, 1, 4)"), ("Dnd", "background-color", "rgb(0, 1, 5)"),
            ("Unknown", "background-color", "rgb(0, 1, 6)"), ("Offline", "border-top-color", "rgb(0, 1, 7)"),
            ("Blocked", "border-top-color", "rgb(0, 1, 8)"),
        };

        foreach (var (name, property, value) in presenceColors)
        {
            await Expect(Presence($"cv-pre-{name}")).ToHaveCSSAsync(property, value);
        }
    }

    [TestMethod]
    public async Task BaseParameters_VisibilityAriaLabelRoleAndHtmlAttributes_LandOnTheRoot()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Root("vis-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Root("vis-collapsed")).ToHaveCSSAsync("display", "none");

        await Expect(Root("aria")).ToHaveAttributeAsync("role", "group");
        await Expect(Root("aria")).ToHaveAttributeAsync("aria-label", "Profile of Named");
        await Expect(Root("aria")).ToHaveAttributeAsync("data-test", "prs-attr");

        await Expect(Root("role")).ToHaveAttributeAsync("role", "listitem");
        await Expect(Root("role")).ToHaveAttributeAsync("aria-label", "Item");

        await Expect(Root("basic-initials")).Not.ToHaveAttributeAsync("role", Any);
    }

    [TestMethod]
    public async Task Unmount_RemovingPersonas_LeavesNoErrorOnThePageOrTheServer()
    {
        await OpenAsync("/e2e/persona");

        await Expect(Page.Locator("#unmount-host .bit-prs")).ToHaveCountAsync(2);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host .bit-prs")).ToHaveCountAsync(2);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#unmount-host")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
