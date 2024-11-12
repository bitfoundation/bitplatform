var BitButil = BitButil || {};

(function (butil: any) {
    butil.userAgent = {
        extract,
    };

    function extract() {
        var nav = window.navigator;
        var ua = nav.userAgent || '';

        var opera = (window as any).operamini || (window as any).opera;
        var brave = (nav as any).brave;

        var data;
        var arch = ua;
        var description = [];
        var prerelease = null;
        var version = opera && typeof opera.version == 'function' && opera.version();

        var layout = getLayout([
            { 'label': 'EdgeHTML', 'pattern': 'Edge' },
            'Trident',
            { 'label': 'WebKit', 'pattern': 'AppleWebKit' },
            'iCab',
            'Presto',
            'NetFront',
            'Tasman',
            'KHTML',
            'Gecko'
        ]);
        var name = getName([
            'Adobe AIR',
            'Arora',
            'Avant Browser',
            'Breach',
            'Camino',
            'Electron',
            'Epiphany',
            'Fennec',
            'Flock',
            'Galeon',
            'GreenBrowser',
            'iCab',
            'Iceweasel',
            'K-Meleon',
            'Konqueror',
            'Lunascape',
            'Maxthon',
            { 'label': 'Microsoft Edge', 'pattern': '(?:Edge|Edg|EdgA|EdgiOS)' },
            'Midori',
            'Nook Browser',
            'PaleMoon',
            'PhantomJS',
            'Raven',
            'Rekonq',
            'RockMelt',
            { 'label': 'Samsung Internet', 'pattern': 'SamsungBrowser' },
            'SeaMonkey',
            { 'label': 'Silk', 'pattern': '(?:Cloud9|Silk-Accelerated)' },
            'Sleipnir',
            'SlimBrowser',
            { 'label': 'SRWare Iron', 'pattern': 'Iron' },
            'Sunrise',
            'Swiftfox',
            'Vivaldi',
            'Waterfox',
            'WebPositive',
            { 'label': 'Yandex Browser', 'pattern': 'YaBrowser' },
            { 'label': 'UC Browser', 'pattern': 'UCBrowser' },
            'Opera Mini',
            { 'label': 'Opera Mini', 'pattern': 'OPiOS' },
            'Opera',
            { 'label': 'Opera', 'pattern': 'OPR' },
            'Chromium',
            'Chrome',
            { 'label': 'Chrome', 'pattern': '(?:HeadlessChrome)' },
            { 'label': 'Chrome Mobile', 'pattern': '(?:CriOS|CrMo)' },
            { 'label': 'Firefox', 'pattern': '(?:Firefox|Minefield)' },
            { 'label': 'Firefox for iOS', 'pattern': 'FxiOS' },
            { 'label': 'IE', 'pattern': 'IEMobile' },
            { 'label': 'IE', 'pattern': 'MSIE' },
            'Safari'
        ]);
        var product = getProduct([
            { 'label': 'BlackBerry', 'pattern': 'BB10' },
            'BlackBerry',
            { 'label': 'Galaxy S', 'pattern': 'GT-I9000' },
            { 'label': 'Galaxy S2', 'pattern': 'GT-I9100' },
            { 'label': 'Galaxy S3', 'pattern': 'GT-I9300' },
            { 'label': 'Galaxy S4', 'pattern': 'GT-I9500' },
            { 'label': 'Galaxy S5', 'pattern': 'SM-G900' },
            { 'label': 'Galaxy S6', 'pattern': 'SM-G920' },
            { 'label': 'Galaxy S6 Edge', 'pattern': 'SM-G925' },
            { 'label': 'Galaxy S7', 'pattern': 'SM-G930' },
            { 'label': 'Galaxy S7 Edge', 'pattern': 'SM-G935' },
            'Google TV',
            'Lumia',
            'iPad',
            'iPod',
            'iPhone',
            'Kindle',
            { 'label': 'Kindle Fire', 'pattern': '(?:Cloud9|Silk-Accelerated)' },
            'Nexus',
            'Nook',
            'PlayBook',
            'PlayStation Vita',
            'PlayStation',
            'TouchPad',
            'Transformer',
            { 'label': 'Wii U', 'pattern': 'WiiU' },
            'Wii',
            'Xbox One',
            { 'label': 'Xbox 360', 'pattern': 'Xbox' },
            'Xoom'
        ]);
        var manufacturer = getManufacturer({
            'Apple': { 'iPad': 1, 'iPhone': 1, 'iPod': 1 },
            'Alcatel': {},
            'Archos': {},
            'Amazon': { 'Kindle': 1, 'Kindle Fire': 1 },
            'Asus': { 'Transformer': 1 },
            'Barnes & Noble': { 'Nook': 1 },
            'BlackBerry': { 'PlayBook': 1 },
            'Google': { 'Google TV': 1, 'Nexus': 1 },
            'HP': { 'TouchPad': 1 },
            'HTC': {},
            'Huawei': {},
            'Lenovo': {},
            'LG': {},
            'Microsoft': { 'Xbox': 1, 'Xbox One': 1 },
            'Motorola': { 'Xoom': 1 },
            'Nintendo': { 'Wii U': 1, 'Wii': 1 },
            'Nokia': { 'Lumia': 1 },
            'Oppo': {},
            'Samsung': { 'Galaxy S': 1, 'Galaxy S2': 1, 'Galaxy S3': 1, 'Galaxy S4': 1 },
            'Sony': { 'PlayStation': 1, 'PlayStation Vita': 1 },
            'Xiaomi': { 'Mi': 1, 'Redmi': 1 }
        });
        var os = getOS([
            'Windows Phone',
            'KaiOS',
            'Android',
            'CentOS',
            { 'label': 'Chrome OS', 'pattern': 'CrOS' },
            'Debian',
            { 'label': 'DragonFly BSD', 'pattern': 'DragonFly' },
            'Fedora',
            'FreeBSD',
            'Gentoo',
            'Haiku',
            'Kubuntu',
            'Linux Mint',
            'OpenBSD',
            'Red Hat',
            'SuSE',
            'Ubuntu',
            'Xubuntu',
            'Cygwin',
            'Symbian OS',
            'hpwOS',
            'webOS ',
            'webOS',
            'Tablet OS',
            'Tizen',
            'Linux',
            'Mac OS X',
            'Macintosh',
            'Mac',
            'Windows 98;',
            'Windows '
        ]);

        layout && (layout = [layout]);

        // Detect Android products.
        if (/\bAndroid\b/.test(os) && !product &&
            (data = /\bAndroid[^;]*;(.*?)(?:Build|\) AppleWebKit)\b/i.exec(ua))) {
            product = trim(data[1])
                // Replace any language codes (eg. "en-US").
                .replace(/^[a-z]{2}-[a-z]{2};\s*/i, '')
                || null;
        }

        // Detect product names that contain their manufacturer's name.
        if (manufacturer && !product) {
            product = getProduct([manufacturer]);
        } else if (manufacturer && product) {
            product = product
                .replace(RegExp('^(' + qualify(manufacturer) + ')[-_.\\s]', 'i'), manufacturer + ' ')
                .replace(RegExp('^(' + qualify(manufacturer) + ')[-_.]?(\\w)', 'i'), manufacturer + ' $2');
        }

        // Detect simulators.
        if (/\bSimulator\b/i.test(ua)) {
            product = (product ? product + ' ' : '') + 'Simulator';
        }

        // Detect Opera Mini 8+ running in Turbo/Uncompressed mode on iOS.
        if (name == 'Opera Mini' && /\bOPiOS\b/.test(ua)) {
            description.push('running in Turbo/Uncompressed mode');
        }

        // Detect iOS.
        else if (/^iP/.test(product)) {
            name || (name = 'Safari');
            os = 'iOS' + ((data = / OS ([\d_]+)/i.exec(ua))
                ? ' ' + data[1].replace(/_/g, '.')
                : '');
        }

        // Detect Android browsers.
        else if ((manufacturer && manufacturer != 'Google' &&
            ((/Chrome/.test(name) && !/\bMobile Safari\b/i.test(ua)) || /\bVita\b/.test(product))) ||
            (/\bAndroid\b/.test(os) && /^Chrome/.test(name) && /\bVersion\//i.test(ua))) {
            name = 'Android Browser';
            os = /\bAndroid\b/.test(os) ? os : 'Android';
        }

        // Detect false positives for Firefox/Safari.
        else if (!name || (data = !/\bMinefield\b/i.test(ua) && /\b(?:Firefox|Safari)\b/.exec(name))) {
            // Escape the `/` for Firefox 1.
            if (name && !product && /[\/,]|^[^(]+?\)/.test(ua.slice(ua.indexOf(data + '/') + 8))) {
                // Clear name of false positives.
                name = null;
            }
            // Reassign a generic name.
            if ((data = product || manufacturer || os) &&
                (product || manufacturer || /\b(?:Android|Symbian OS|Tablet OS|webOS)\b/.test(os))) {
                name = /[a-z]+(?: Hat)?/i.exec(/\bAndroid\b/.test(os) ? os : data) + ' Browser';
            }
        }

        // Detect non-Opera (Presto-based) versions (order is important).
        if (!version) {
            version = getVersion([
                '(?:Cloud9|CriOS|CrMo|Edge|Edg|EdgA|EdgiOS|FxiOS|HeadlessChrome|IEMobile|Iron|Opera ?Mini|OPiOS|OPR|Raven|SamsungBrowser|Silk(?!/[\\d.]+$)|UCBrowser|YaBrowser)',
                'Version',
                qualify(name),
                '(?:Firefox|Minefield|NetFront)'
            ]);
        }

        // Detect stubborn layout engines.
        if ((data =
            layout == 'iCab' && parseFloat(version) > 3 && 'WebKit' ||
            /\bOpera\b/.test(name) && (/\bOPR\b/.test(ua) ? 'Blink' : 'Presto') ||
            /\b(?:Midori|Nook|Safari)\b/i.test(ua) && !/^(?:Trident|EdgeHTML)$/.test(layout) && 'WebKit' ||
            !layout && /\bMSIE\b/i.test(ua) && (os == 'Mac OS' ? 'Tasman' : 'Trident') ||
            layout == 'WebKit' && /\bPlayStation\b(?! Vita\b)/i.test(name) && 'NetFront'
        )) {
            layout = [data];
        }

        // Detect Firefox Mobile.
        if (name == 'Fennec' || name == 'Firefox' && /\b(?:Android|Firefox OS|KaiOS)\b/.test(os)) {
            name = 'Firefox Mobile';
        }
        // Add mobile postfix.
        else if ((/^(?:Chrome|IE|Opera)$/.test(name) || name && !product && !/Browser|Mobi/.test(name)) &&
            (os == 'Windows CE' || /Mobi/i.test(ua))) {
            name += ' Mobile';
        }

        // Detect WebKit Nightly and approximate Chrome/Safari versions.
        if ((data = (/\bAppleWebKit\/([\d.]+\+?)/i.exec(ua) || 0)[1])) {
            // Correct build number for numeric comparison.
            // (e.g. "532.5" becomes "532.05")
            data = [parseFloat(data.replace(/\.(\d)$/, '.0$1')), data];
            // Nightly builds are postfixed with a "+".
            if (name == 'Safari' && data[1].slice(-1) == '+') {
                name = 'WebKit Nightly';
                prerelease = 'alpha';
                version = data[1].slice(0, -1);
            }
            // Clear incorrect browser versions.
            else if (version == data[1] ||
                version == (data[2] = (/\bSafari\/([\d.]+\+?)/i.exec(ua) || 0)[1])) {
                version = null;
            }
            // Use the full Chrome version when available.
            data[1] = (/\b(?:Headless)?Chrome\/([\d.]+)/i.exec(ua) || 0)[1];
            // Detect Blink layout engine.
            if (data[0] == 537.36 && data[2] == 537.36 && parseFloat(data[1]) >= 28 && layout == 'WebKit') {
                layout = ['Blink'];
            }
            // Detect JavaScriptCore.
            // http://stackoverflow.com/questions/6768474/how-can-i-detect-which-javascript-engine-v8-or-jsc-is-used-at-runtime-in-androi
            var likeChrome = /\bChrome\b/.test(ua) && !/internal|\n/i.test(Object.prototype.toString.toString());
            if ((!likeChrome && !data[1])) {
                layout && (layout[1] = 'like Safari');
                data = (data = data[0], data < 400 ? 1 : data < 500 ? 2 : data < 526 ? 3 : data < 533 ? 4 : data < 534 ? '4+' : data < 535 ? 5 : data < 537 ? 6 : data < 538 ? 7 : data < 601 ? 8 : data < 602 ? 9 : data < 604 ? 10 : data < 606 ? 11 : data < 608 ? 12 : '12');
            } else {
                layout && (layout[1] = 'like Chrome');
                data = data[1] || (data = data[0], data < 530 ? 1 : data < 532 ? 2 : data < 532.05 ? 3 : data < 533 ? 4 : data < 534.03 ? 5 : data < 534.07 ? 6 : data < 534.10 ? 7 : data < 534.13 ? 8 : data < 534.16 ? 9 : data < 534.24 ? 10 : data < 534.30 ? 11 : data < 535.01 ? 12 : data < 535.02 ? '13+' : data < 535.07 ? 15 : data < 535.11 ? 16 : data < 535.19 ? 17 : data < 536.05 ? 18 : data < 536.10 ? 19 : data < 537.01 ? 20 : data < 537.11 ? '21+' : data < 537.13 ? 23 : data < 537.18 ? 24 : data < 537.24 ? 25 : data < 537.36 ? 26 : layout != 'Blink' ? '27' : '28');
            }
            // Add the postfix of ".x" or "+" for approximate versions.
            layout && (layout[1] += ' ' + (data += typeof data == 'number' ? '.x' : /[.+]/.test(data) ? '' : '+'));
            // Obscure version for some Safari 1-2 releases.
            if (name == 'Safari' && (!version || parseInt(version) > 45)) {
                version = data;
            } else if (name == 'Chrome' && /\bHeadlessChrome/i.test(ua)) {
                description.unshift('headless');
            }
        }

        // Detect Opera desktop modes.
        if (name == 'Opera' && (data = /\bzbov|zvav$/.exec(os))) {
            name += ' ';
            description.unshift('desktop mode');
            if (data == 'zvav') {
                name += 'Mini';
                version = null;
            } else {
                name += 'Mobile';
            }
            os = os.replace(RegExp(' *' + data + '$'), '');
        }
        // Detect Chrome desktop mode.
        else if (name == 'Safari' && /\bChrome\b/.exec(layout && layout[1])) {
            description.unshift('desktop mode');
            name = 'Chrome Mobile';
            version = null;

            if (/\bOS X\b/.test(os)) {
                manufacturer = 'Apple';
                os = 'iOS 4.3+';
            } else {
                os = null;
            }
        }

        // Strip incorrect OS versions.
        if (version && version.indexOf((data = /[\d.]+$/.exec(os))) == 0 &&
            ua.indexOf('/' + data + '-') > -1) {
            os = trim(os.replace(data, ''));
        }

        // Ensure OS does not include the browser name.
        if (os && os.indexOf(name) != -1 && !RegExp(name + ' OS').test(os)) {
            os = os.replace(RegExp(' *' + qualify(name) + ' *'), '');
        }

        // Add layout engine.
        if (layout && !/\b(?:Avant|Nook)\b/.test(name) && (
            /Browser|Lunascape|Maxthon/.test(name) ||
            name != 'Safari' && /^iOS/.test(os) && /\bSafari\b/.test(layout[1]) ||
            /^(?:Adobe|Arora|Breach|Midori|Opera|Phantom|Rekonq|Rock|Samsung Internet|Sleipnir|SRWare Iron|Vivaldi|Web)/.test(name) && layout[1])) {
            // Don't add layout details to description if they are falsey.
            (data = layout[layout.length - 1]) && description.push(data);
        }

        // Combine contextual information.
        if (description.length) {
            description = ['(' + description.join('; ') + ')'];
        }

        // Append manufacturer to description.
        if (manufacturer && product && product.indexOf(manufacturer) < 0) {
            description.push('on ' + manufacturer);
        }

        // Append product to description.
        if (product) {
            description.push((/^on /.test(description[description.length - 1]) ? '' : 'on ') + product);
        }

        // Parse the OS into an object.
        if (os) {
            data = / ([\d.+]+)$/.exec(os);
            var isSpecialCasedOS = data && os.charAt(os.length - data[0].length - 1) == '/';
            os = {
                'architecture': 32,
                'family': (data && !isSpecialCasedOS) ? os.replace(data[0], '') : os,
                'version': data ? data[1] : null,
                'toString': function () {
                    var version = this.version;
                    return this.family + ((version && !isSpecialCasedOS) ? ' ' + version : '') + (this.architecture == 64 ? ' 64-bit' : '');
                }
            };
        }

        // Add browser/OS architecture.
        if ((data = /\b(?:AMD|IA|Win|WOW|x86_|x)64\b/i.exec(arch)) && !/\bi686\b/i.test(arch)) {
            if (os) {
                os.architecture = 64;
                os.family = os.family.replace(RegExp(' *' + data), '');
            }
            if (
                name && (/\bWOW64\b/i.test(ua) ||
                    (/\w(?:86|32)$/.test((nav as any).cpuClass || nav.platform) && !/\bWin64; x64\b/i.test(ua)))
            ) {
                description.unshift('32-bit');
            }
        }
        // Chrome 39 and above on OS X is always 64-bit.
        else if (os && /^OS X/.test(os.family) && name == 'Chrome' && parseFloat(version) >= 39) {
            os.architecture = 64;
        }

        var userAgentProps = {} as any;
        userAgentProps.name = brave ? 'Brave' : name;
        userAgentProps.version = name && version;
        userAgentProps.prerelease = prerelease;
        userAgentProps.layout = layout && layout[0];
        userAgentProps.manufacturer = manufacturer;
        userAgentProps.product = product;
        userAgentProps.osName = os?.family;
        userAgentProps.osVersion = os?.version;
        userAgentProps.osArchitecture = os?.architecture;
        userAgentProps.description = ua;
        userAgentProps.userAgentValue = ua;

        if (userAgentProps.version) {
            description.unshift(version);
        }
        if (userAgentProps.name) {
            description.unshift(name);
        }
        if (os && name && !(os == String(os).split(' ')[0] && (os == name.split(' ')[0] || product))) {
            description.push(product ? '(' + os + ')' : 'on ' + os);
        }
        if (description.length) {
            userAgentProps.description = description.join(' ');
        }
        
        return userAgentProps;

        function getLayout(guesses) {
            return reduce(guesses, function (result, guess) {
                return result || RegExp('\\b' + (
                    guess.pattern || qualify(guess)
                ) + '\\b', 'i').exec(ua) && (guess.label || guess);
            });
        }

        function getManufacturer(guesses) {
            return reduce(guesses, function (result, value, key) {
                // Lookup the manufacturer by product or scan the UA for the manufacturer.
                return result || (
                    value[product] ||
                    value[(/^[a-z]+(?: +[a-z]+\b)*/i.exec(product)) as any] ||
                    RegExp('\\b' + qualify(key) + '(?:\\b|\\w*\\d)', 'i').exec(ua)
                ) && key;
            });
        }

        function getName(guesses) {
            return reduce(guesses, function (result, guess) {
                return result || RegExp('\\b' + (
                    guess.pattern || qualify(guess)
                ) + '\\b', 'i').exec(ua) && (guess.label || guess);
            });
        }

        function getOS(guesses) {
            return reduce(guesses, function (result, guess) {
                var pattern = guess.pattern || qualify(guess);
                if (!result && (result =
                    RegExp('\\b' + pattern + '(?:/[\\d.]+|[ \\w.]*)', 'i').exec(ua)
                )) {
                    result = cleanupOS(result, pattern, guess.label || guess);
                }
                return result;
            });

            function cleanupOS(os, pattern, label) {
                var data = {
                    '10.0': '10',
                    '6.4': '10 Technical Preview',
                    '6.3': '8.1',
                    '6.2': '8',
                    '6.1': 'Server 2008 R2 / 7',
                    '6.0': 'Server 2008 / Vista',
                    '5.2': 'Server 2003 / XP 64-bit',
                    '5.1': 'XP',
                    '5.01': '2000 SP1',
                    '5.0': '2000',
                    '4.0': 'NT',
                    '4.90': 'ME'
                };


                if (pattern && label && /^Win/i.test(os) && !/^Windows Phone /i.test(os) &&
                    (data = data[(/[\d.]+$/.exec(os)) as any])) {
                    os = 'Windows ' + data;
                }

                os = String(os);

                if (pattern && label) {
                    os = os.replace(RegExp(pattern, 'i'), label);
                }

                os = format(
                    os.replace(/ ce$/i, ' CE')
                        .replace(/\bhpw/i, 'web')
                        .replace(/\bMacintosh\b/, 'Mac OS')
                        .replace(/_PowerPC\b/i, ' OS')
                        .replace(/\b(OS X) [^ \d]+/i, '$1')
                        .replace(/\bMac (OS X)\b/, '$1')
                        .replace(/\/(\d)/, ' $1')
                        .replace(/_/g, '.')
                        .replace(/(?: BePC|[ .]*fc[ \d.]+)$/i, '')
                        .replace(/\bx86\.64\b/gi, 'x86_64')
                        .replace(/\b(Windows Phone) OS\b/, '$1')
                        .replace(/\b(Chrome OS \w+) [\d.]+\b/, '$1')
                        .split(' on ')[0]
                );

                return os;
            }
        }

        function getProduct(guesses) {
            return reduce(guesses, function (result, guess) {
                var pattern = guess.pattern || qualify(guess);
                if (!result && (result =
                    RegExp('\\b' + pattern + ' *\\d+[.\\w_]*', 'i').exec(ua) ||
                    RegExp('\\b' + pattern + ' *\\w+-[\\w]*', 'i').exec(ua) ||
                    RegExp('\\b' + pattern + '(?:; *(?:[a-z]+[_-])?[a-z]+\\d+|[^ ();-]*)', 'i').exec(ua)
                )) {
                    // Split by forward slash and append product version if needed.
                    if ((result = String((guess.label && !RegExp(pattern, 'i').test(guess.label)) ? guess.label : result).split('/'))[1] && !/[\d.]+/.test(result[0])) {
                        result[0] += ' ' + result[1];
                    }
                    // Correct character case and cleanup string.
                    guess = guess.label || guess;
                    result = format(result[0]
                        .replace(RegExp(pattern, 'i'), guess)
                        .replace(RegExp('; *(?:' + guess + '[_-])?', 'i'), ' ')
                        .replace(RegExp('(' + guess + ')[-_.]?(\\w)', 'i'), '$1 $2'));
                }
                return result;
            });
        }

        function getVersion(patterns) {
            return reduce(patterns, function (result, pattern) {
                return result || (RegExp(pattern +
                    '(?:-[\\d.]+/|(?: for [\\w-]+)?[ /-])([\\d.]+[^ ();/_-]*)', 'i').exec(ua) || 0)[1] || null;
            });
        }
    }

    function capitalize(value: any) {
        value = String(value);
        return value.charAt(0).toUpperCase() + value.slice(1);
    }

    function each(obj: any, callback: any) {
        var index = -1,
            length = obj ? obj.length : 0;

        if (typeof length == 'number' && length > -1 && length <= Math.pow(2, 53) - 1) {
            while (++index < length) {
                callback(obj[index], index, obj);
            }
        } else {
            forOwn(obj, callback);
        }
    }

    function format(value: any) {
        value = trim(value);
        return /^(?:webOS|i(?:OS|P))/.test(value)
            ? value
            : capitalize(value);
    }

    function forOwn(obj: any, callback: any) {
        for (var key in obj) {
            if (Object.prototype.hasOwnProperty.call(obj, key)) {
                callback(obj[key], key, obj);
            }
        }
    }

    function qualify(value: any) {
        return String(value).replace(/([ -])(?!$)/g, '$1?');
    }

    function reduce(array: any, callback: any) {
        var accumulator = null;
        each(array, function (value: any, index: any) {
            accumulator = callback(accumulator, value, index, array);
        });
        return accumulator;
    }

    function trim(value: any) {
        return String(value).replace(/^ +| +$/g, '');
    }

}(BitButil));