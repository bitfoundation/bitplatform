# Vazirmatn

These files draw Persian and Arabic text. A browser fetches them only for a page in Persian or Arabic, and only for the characters that page shows. Other languages never download them, unless the service worker runs in the `FullOffline` mode described below.

How that works:

- Client.Core's `Styles/_rtl-fonts.scss` adds Vazirmatn to the font stack only under `:lang(fa)` and `:lang(ar)`, so no other culture refers to these files.
- Each `@font-face` declares a `unicode-range`, the characters its file covers. The browser downloads a face only when the page renders one of those characters with that font family.
- The service worker keeps to this in the default `NoPrerender` mode, which caches only what a page requests. In `FullOffline` mode it downloads every asset when it installs, these files included, so the app also works offline in Persian and Arabic. Other languages still never use them.

The files:

- `vazirmatn-arabic.woff2`: only the Arabic-script letters (U+0600 to U+06FF plus the other Arabic blocks), as one variable file for weights 300 to 800. Latin letters and digits keep coming from the theme's own font.
- `vazirmatn-fd-digits-300.woff2` to `vazirmatn-fd-digits-800.woff2`: only the ten digits (U+0030 to U+0039), cut from Vazirmatn's Farsi-digits version, about 4 KB per weight. Persian puts this face first, so `1` renders as ۱ while the text keeps the ASCII digit, which leaves copied text, search and input values unchanged. Arabic leaves it out, because Arabic-Indic digits (٤٥٦) are drawn differently from the Persian ones (۴۵۶). Elements with the `app-latin-digits` class, and email, URL and password fields, keep Latin digits.
- `OFL.txt`: the SIL Open Font License that Vazirmatn is released under.
