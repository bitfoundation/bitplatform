var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The legacy user-agent *string* parser, kept out of the userAgent module on purpose: the
    // Client Hints members there are a handful of property reads, while the tables below are the
    // bulk of what this pair of modules weighs. Splitting them means an app that only asks whether
    // the client is mobile does not download the parser it never calls.
    //
    // Every table here is ordered and read top to bottom, stopping at the first row that matches,
    // because these strings are built out of each other: a Chromium browser carries `Chrome/`, an
    // Android WebView carries a vestigial `Version/4.0`, the Meta Quest browser carries
    // `SamsungBrowser/4.0`, an in-app webview carries its host browser's whole string, and nearly
    // everything ends in `Safari/537.36`. A row placed below a more general one never fires.
    //
    // What is deliberately *not* here is the pre-Blink world - Presto Opera, EdgeHTML, Tasman,
    // KHTML, the AppleWebKit-build-to-version tables. Every Chromium has reported AppleWebKit/537.36
    // and every Safari 605.1.15 for a decade, so those tables only ever fire on a string with no
    // version token at all, where they invent one (an iOS 18 in-app webview used to come back as
    // "Safari 11.x"). A missing version is reported as missing instead.
    butil.userAgentParser = { extract };

    // [name, pattern]. Group 1 is the version where the string carries one. `$1` in a name is
    // replaced by group 1, which is how one row covers a family of spellings.
    const BROWSERS: [string, RegExp][] = [
        // Crawlers first, for the log-analysis case: they append their own token to a full Chrome
        // or Safari string, so without these rows they come back as the browser they imitate.
        ['Googlebot', /\bGooglebot\/([\d.]+)/],
        ['Bingbot', /\bbingbot\/([\d.]+)/],
        // In-app webviews next: they are the host browser's string plus one token of their own,
        // so every row below this point also matches them.
        ['Facebook', /\bFBAV\/([\d.]+)|\bFBAN\/\w+/],
        ['Instagram', /\bInstagram (\d[\d.]*)/],
        ['WeChat', /\bMicroMessenger\/([\d.]+)/i],
        ['LINE', /\bLine\/([\d.]+)/],
        ['Android WebView', /;\s*wv\).*\bChrome\/([\d.]+)/],
        ['Meta Quest Browser', /\bOculusBrowser\/([\d.]+)/],
        // Edg (Chromium), Edge (EdgeHTML), EdgA (Android), EdgiOS (iOS) - one brand, four tokens.
        ['Microsoft Edge', /\bEdg(?:e|A|iOS)?\/([\d.]+)/],
        ['Opera Mini', /\b(?:Opera Mini|OPiOS)\/([\d.]+)/],
        ['Opera Touch', /\bOPT\/([\d.]+)/],
        // Chromium Opera says OPR; the Presto-era string put the real version behind `Version/`.
        ['Opera', /\bOPR\/([\d.]+)|\bOpera\/(?:9\.80.*Version\/)?([\d.]+)/],
        ['Samsung Internet', /\bSamsungBrowser\/([\d.]+)/],
        ['Silk', /\bSilk\/([\d.]+)/],
        ['Vivaldi', /\bVivaldi\/([\d.]+)/],
        ['Yandex Browser', /\bYaBrowser\/([\d.]+)/],
        ['UC Browser', /\bUCBrowser\/([\d.]+)/],
        ['QQ Browser', /\b(?:M?QQBrowser)\/([\d.]+)/],
        ['Huawei Browser', /\bHuaweiBrowser\/([\d.]+)/],
        ['Whale', /\bWhale\/([\d.]+)/],
        ['DuckDuckGo', /\b(?:DuckDuckGo|Ddg)\/([\d.]+)/],
        ['Electron', /\bElectron\/([\d.]+)/],
        ['Firefox Focus', /\bFocus\/([\d.]+)/],
        ['Firefox for iOS', /\bFxiOS\/([\d.]+)/],
        ['Chrome', /\b(?:CriOS|CrMo)\/([\d.]+)/],
        ['Chrome', /\bHeadlessChrome\/([\d.]+)/],
        ['Chromium', /\bChromium\/([\d.]+)/],
        ['Chrome', /\bChrome\/([\d.]+)/],
        ['SeaMonkey', /\bSeaMonkey\/([\d.]+)/],
        ['Pale Moon', /\bPaleMoon\/([\d.]+)/],
        ['Waterfox', /\bWaterfox\/([\d.]+)/],
        // Firefox is the one that still marks a channel in the string it sends: "134.0b3".
        ['Firefox', /\bFirefox\/([\d.]+(?:[ab]\d*)?)/],
        ['IE Mobile', /\bIEMobile[/ ]([\d.]+)/],
        ['IE', /\bMSIE ([\d.]+)/],
        // IE 11 stopped saying MSIE and moved its version behind `rv:`, so the one version of IE
        // still turning up in logs is the one an MSIE pattern alone cannot name.
        ['IE', /\bTrident\/[\d.]+;.*\brv:([\d.]+)/],
        // The old Android stock browser: WebKit with a `Version/` and no Chromium token at all.
        ['Android Browser', /^(?!.*\bChrome\/).*\bAndroid\b.*\bVersion\/([\d.]+)/],
        // Two big screens that end their string with Safari's name while being neither Safari nor
        // a phone: a Samsung TV (which reports its Chromium build ahead of "TV") and a console.
        ['Samsung TV Browser', /\bTizen [\d.]+\).*?\b([\d.]+)\/[\d.]+ TV\b/],
        ['PlayStation Browser', /\bPlayStation\b.*\bVersion\/([\d.]+)/],
        ['Nintendo Browser', /\bNintendoBrowser\/([\d.]+)/],
        ['Safari', /\bVersion\/([\d.]+).*\bSafari\//],
        // A WebKit view with no `Version/` - an iOS webview, a TV, a console. The name is all the
        // string gives; the version stays null rather than being guessed from the WebKit build.
        ['Safari', /\bSafari\/[\d.]+/]
    ];

    const ENGINES: [string, RegExp][] = [
        ['Presto', /\bPresto\//],
        ['EdgeHTML', /\bEdge\/[\d.]+/],
        ['Trident', /\bTrident\//],
        ['Goanna', /\bGoanna\//],
        // "like Gecko" is in half the strings ever written, so real Gecko is the one with a build.
        ['Gecko', /\bGecko\/\d/],
        // Blink's marker is the frozen WebKit build it forked at: every Chromium since Chrome 28
        // reports exactly 537.36, and nothing else does.
        ['Blink', /\bAppleWebKit\/537\.36\b/],
        ['WebKit', /\bAppleWebKit\//]
    ];

    // [family, pattern]. Group 1 is the version; `$1` as the family means group 1 names it.
    const SYSTEMS: [string, RegExp][] = [
        // Ordered by how much a string lies about itself: Windows Phone also says "Android", KaiOS
        // also says "Android", Android also says "Linux", and iOS also says "Mac OS X".
        ['Windows Phone', /\bWindows Phone (?:OS )?([\d.]+)/],
        ['KaiOS', /\bKaiOS\/([\d.]+)/i],
        ['Android', /\bAndroid[ /-]([\d.]+)/],
        ['Android', /\bAndroid\b/],
        ['iOS', /\b(?:iPhone )?OS ([\d_]+) like Mac OS X/],
        ['iOS', /\b(?:iPhone|iPad|iPod)\b/],
        ['Chrome OS', /\bCrOS \S+ ([\d.]+)/],
        ['Chrome OS', /\bCrOS\b/],
        // NT 10.0 is Windows 10 *and* Windows 11 - the string cannot tell them apart. Only
        // UserAgent.GetHighEntropyValues("platformVersion") can.
        ['Windows', /\bWindows NT ([\d.]+)/],
        ['Windows', /\bWindows\b/],
        ['Tizen', /\bTizen[ /]([\d.]+)/],
        ['HarmonyOS', /\b(?:HarmonyOS|OpenHarmony)\b/i],
        ['webOS', /\bWeb0S\b|\bwebOS\/?([\d.]+)?/i],
        ['BlackBerry', /\bBB10\b|\bBlackBerry\b/],
        ['$1', /\b(Ubuntu|Kubuntu|Xubuntu|Linux Mint|Fedora|Debian|CentOS|Red Hat|SUSE|Gentoo|Manjaro|FreeBSD|OpenBSD|NetBSD)\b/],
        ['macOS', /\bMac OS X (\d+[\d._]*)/],
        ['macOS', /\bMac(?:intosh)?\b/],
        ['Linux', /\bLinux\b|\bX11\b/]
    ];

    const BITS_64 = /\b(?:x86[_.-]64|x64|Win64|WOW64|amd64|aarch64|arm64|ppc64)\b/i;
    const BITS_32 = /\b(?:i[3-6]86|x86|armv\d|Win32)\b/i;

    const WINDOWS_VERSIONS: { [nt: string]: string } = {
        '10.0': '10', '6.3': '8.1', '6.2': '8', '6.1': '7', '6.0': 'Vista', '5.2': 'XP', '5.1': 'XP', '5.0': '2000'
    };

    // [product, manufacturer, pattern] for devices that name themselves rather than sitting in
    // Android's model slot. `$1` is group 1 of the pattern.
    const DEVICES: [string, string | null, RegExp][] = [
        ['iPhone', 'Apple', /\biPhone\b/],
        ['iPad', 'Apple', /\biPad\b/],
        ['iPod', 'Apple', /\biPod\b/],
        ['Quest $1', 'Meta', /\bQuest (\d+)\b/],
        ['PlayStation $1', 'Sony', /\bPlayStation (Vita|\d+)\b/],
        ['PlayStation', 'Sony', /\bPlayStation\b/],
        ['Xbox $1', 'Microsoft', /\bXbox (Series [XS]|One)\b/],
        ['Xbox', 'Microsoft', /\bXbox\b/],
        ['Nintendo Switch', 'Nintendo', /\bNintendo Switch\b/],
        ['Kindle Fire', 'Amazon', /\bKF\w+\b/],
        ['Kindle', 'Amazon', /\bKindle\b/]
    ];

    // Model-number prefixes that identify a maker. A model this does not recognize gets no
    // manufacturer rather than a guess.
    const VENDORS: [string, RegExp][] = [
        ['Samsung', /^(?:SAMSUNG[- ])?(?:SM|GT|SCH|SPH|SGH)-|^Galaxy\b/i],
        ['Google', /^(?:Pixel|Nexus)\b/i],
        ['Xiaomi', /^(?:Xiaomi|Mi\b|Redmi|POCO)/i],
        ['Huawei', /^HUAWEI/i],
        ['Honor', /^HONOR/i],
        ['OnePlus', /^OnePlus/i],
        ['Oppo', /^(?:OPPO|CPH\d)/i],
        ['Vivo', /^vivo/i],
        ['Realme', /^(?:realme|RMX\d)/i],
        ['Motorola', /^(?:moto|XT\d{4})/i],
        ['Nokia', /^Nokia/i],
        ['Sony', /^(?:Xperia|XQ-)/i],
        ['Asus', /^ASUS/i],
        ['Amazon', /^KF/]
    ];

    function extract(userAgentString?: string) {
        const nav = window.navigator;
        const ua = userAgentString || nav.userAgent || '';

        const browser = match(BROWSERS, ua);
        const engine = match(ENGINES, ua);
        const system = match(SYSTEMS, ua);

        let name = browser?.label ?? null;
        const version = browser?.value ?? null;

        // navigator.brave describes the browser this code is running in, so it may only be
        // consulted when the string being parsed is that browser's own: Extract(someLogLine) on a
        // Brave desktop must not report the log line as Brave. Brave is otherwise unidentifiable -
        // it ships Chrome's string on purpose.
        if ((!userAgentString || userAgentString === nav.userAgent) && (nav as any).brave) {
            name = 'Brave';
        }

        // The mobile builds of these carry no token of their own beyond the platform's `Mobi`.
        if (name && /^(?:Chrome|Chromium|Firefox|Opera|Brave|IE)$/.test(name) && /\bMobi/i.test(ua)) {
            name += ' Mobile';
        }

        const osName = system?.label ?? null;
        let osVersion = system?.value ?? null;
        if (osName === 'Windows' && osVersion) osVersion = WINDOWS_VERSIONS[osVersion] ?? osVersion;
        if (osVersion) osVersion = osVersion.replace(/_/g, '.');

        const device = matchDevice(ua, osName);
        let product = device?.product ?? (osName === 'Android' ? androidModel(ua) : null);
        let manufacturer = device?.manufacturer ?? null;
        if (product && !manufacturer) {
            const vendor = match(VENDORS, product);
            if (vendor) {
                manufacturer = vendor.label;
                // "SAMSUNG SM-S918B" names the maker twice once the column beside it does.
                product = product.replace(RegExp('^' + manufacturer + '[- ]', 'i'), '');
            }
        }

        const props = {
            name,
            version,
            prerelease: prerelease(version),
            layout: engine?.label ?? null,
            manufacturer,
            product: product || null,
            osName,
            osVersion,
            osArchitecture: architecture(ua, osName),
            description: ua,
            userAgentValue: ua
        };

        props.description = describe(props, ua);
        return props;
    }

    // First matching row of a table, with `$1` in the label filled in from the match.
    function match(table: [string, RegExp][], value: string) {
        for (const [label, pattern] of table) {
            const found = pattern.exec(value);
            if (!found) continue;
            const group = found[1] ?? found[2] ?? null;
            return { label: label.replace('$1', group ?? ''), value: label.indexOf('$1') < 0 ? group : null };
        }
        return null;
    }

    function matchDevice(ua: string, osName: string | null) {
        for (const [product, manufacturer, pattern] of DEVICES) {
            // "like iPhone OS 7_0_3" is in strings written by devices that are not an iPhone -
            // Windows Phone's among them - so an Apple product only counts on an Apple system.
            if (manufacturer === 'Apple' && osName !== 'iOS') continue;

            const found = pattern.exec(ua);
            if (found) return { product: product.replace('$1', found[1] ?? ''), manufacturer };
        }
        return null;
    }

    // Android puts the model in the platform section, but not always in the same field:
    //   (Linux; Android 13; SM-S918B Build/TP1A.220624.014; wv)
    //   (Linux; U; Android 13; en-US; SM-A536E Build/TP1A.220624.014)
    // Chrome 110+ freezes that field to the literal "K", and older strings put a locale, a form
    // factor, an engine version or the WebView marker in it - none of which is a model, so all of
    // them yield none rather than a plausible-looking wrong answer.
    function androidModel(ua: string) {
        const section = /\bAndroid[^;)]*;([^)]*)\)/.exec(ua);
        if (!section) return null;

        for (const field of section[1].split(';')) {
            const model = field.replace(/\bBuild\/.*/, '').trim();
            if (!model || /[:/]/.test(model)) continue;
            if (/^(?:K|U|wv|VR|Mobile|Tablet|HarmonyOS|[a-z]{2}(?:[-_][a-z]{2,3})?)$/i.test(model)) continue;
            return model;
        }
        return null;
    }

    // Only a suffix on the version itself still marks a channel (Firefox's "133.0b3"). Chrome's
    // channels, Firefox's Developer Edition and Safari Technology Preview are indistinguishable
    // from the release build by the string alone.
    function prerelease(version: string | null) {
        const found = /\d(a|b|pre|dev)\d*$/i.exec(version || '');
        return found ? ({ a: 'alpha', b: 'beta', pre: 'prerelease', dev: 'dev' } as any)[found[1].toLowerCase()] : null;
    }

    // The bitness the string states, plus the two platforms whose bitness is a settled fact:
    // Apple dropped 32-bit support from iOS in 11 and from macOS in Catalina. Everything else -
    // Android, Chrome OS, a bare "Linux" - comes back null rather than defaulting to a width the
    // string never mentioned.
    function architecture(ua: string, osName: string | null) {
        if (BITS_64.test(ua)) return 64;
        if (osName === 'iOS' || osName === 'macOS') return 64;
        if (BITS_32.test(ua)) return 32;
        return null;
    }

    function describe(props: any, ua: string) {
        const parts = [];
        const extras = [];

        if (/\bHeadlessChrome\b/.test(ua)) extras.push('headless');
        // The Chromium a rebranded Chromium is built on is the useful half of its string - it is
        // what decides which web platform features it has - but only where it is news: Edge and
        // the WebView version their build alongside Chrome's, Samsung Internet and Opera do not.
        const chromium = /\bChrome\/([\d.]+)/.exec(ua);
        if (chromium && props.layout === 'Blink' && props.name && !/^(?:Chrome|Chromium|Brave)\b/.test(props.name) &&
            major(chromium[1]) !== major(props.version)) {
            extras.push('like Chrome ' + chromium[1]);
        }

        if (props.name) parts.push(props.name);
        if (props.version) parts.push(props.version);
        if (extras.length) parts.push('(' + extras.join('; ') + ')');

        // The bit-width is worth a reader's attention only where the string actually stated it -
        // "iOS 18.2 64-bit" is noise, since there has never been an iOS device that was not.
        const bits = BITS_64.test(ua) || BITS_32.test(ua) ? ' ' + props.osArchitecture + '-bit' : '';
        const os = props.osName
            ? props.osName + (props.osVersion ? ' ' + props.osVersion : '') + bits
            : null;

        if (props.product) {
            const maker = props.manufacturer && props.product.indexOf(props.manufacturer) < 0 ? props.manufacturer + ' ' : '';
            parts.push('on ' + maker + props.product);
            if (os) parts.push('(' + os + ')');
        } else if (os) {
            parts.push('on ' + os);
        }

        return parts.join(' ') || ua;
    }

    function major(version: string | null) {
        return String(version || '').split('.')[0];
    }
}(BitButil));
