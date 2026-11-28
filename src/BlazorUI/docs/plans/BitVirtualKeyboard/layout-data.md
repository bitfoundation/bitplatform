# BitVirtualKeyboard layout data

Key data for the language layouts of BitVirtualKeyboard. It is the single source of truth for the characters of the built-in language catalog (PLAN.md section 8.2). It was extracted from the research report's verified row data (research-report.md); the original sources are linked per layout.


Each row below was extracted from Windows `kbd*.dll` tables via kbdlayout.info KLC downloads, then cross-checked against CLDR and the national standards. Rows are in scan-code order, not physical order. Row E is the number row (scan 29, then 02-0D), starting at the \`/~ key. Row D runs Q…P (scan 10-19), then `[ ]` (1A-1B), then the scan-2B key (`\` on US; `#` on UK/DE, `*` on FR, `ç` on ES, `,` on TR): it ends row D on ANSI boards, but sits physically at the end of row C, left of Enter, on ISO boards. Row C runs A…;' (scan 1E-28). Row B starts with the ISO extra key (scan 56, absent on ANSI boards; ABNT2 also has scan 73 at the end) and then runs Z…/ (scan 2C-35).

Re-verified on 2026-11-25 for every P0 layout (en, en-GB, fa, ar, he, de, fr legacy, es, ru, tr-Q) by decoding the KLC files again (kbdlayout.info, dll version 10.0.29667.1000) and, for Persian, the raw ISIRI 9147 table on Wikipedia.

`x(dead)` marks a dead key, `◌x` a combining mark and `·` no output. `…` means the keys in between match the letters of the base layer. Every row comes from the cited kbdlayout.info page unless another source is named.

**US QWERTY** ([KBDUS](https://kbdlayout.info/KBDUS/))
```
base  E: ` 1 2 3 4 5 6 7 8 9 0 - =    D: q w e r t y u i o p [ ] \    C: a s d f g h j k l ; '    B: (\) z x c v b n m , . /
shift E: ~ ! @ # $ % ^ & * ( ) _ +    D: Q W E R T Y U I O P { } |    C: A S D F G H J K L : "    B: | Z X C V B N M < > ?
```
**UK** ([KBDUK](https://kbdlayout.info/KBDUK/)). Base differs from US only at the scan-2B key: `#` (Shift `~`), last in row D. Shift row: `E: ¬ ! " £ $ % ^ & * ( ) _ +`, `D: Q…P { } ~`, `C: A…L : @`, `B: | Z…M < > ?`. AltGr: \`→¦, 4→€, e→é, u→ú, i→í, o→ó, a→á, `#`→`\`, with Shift+AltGr giving the capitals (and Shift+AltGr+`#` → `|`).

**French AZERTY, legacy Windows** ([KBDFR](https://kbdlayout.info/KBDFR/))
```
base  E: ² & é " ' ( - è _ ç à ) =    D: a z e r t y u i o p ^(dead) $ *    C: q s d f g h j k l m ù    B: < w x c v b n , ; : !
shift E: · 1 2 3 4 5 6 7 8 9 0 ° +    D: A Z E R T Y U I O P ¨(dead) £ µ    C: Q S D F G H J K L M %    B: > W X C V B N ? . / §
altgr E: · · ~(dead) # { [ | `(dead) \ ^ @ ] }    D: e→€, $→¤
```
On AZERTY the digits need Shift, which matters for numeric-heavy forms.

**French AZERTY, AFNOR "Français normalisé"** ([CLDR fr.xml](https://github.com/unicode-org/cldr/blob/main/keyboards/3.0/fr.xml))
```
none  @ à é è ê ( ) ‘ ’ « » ' ^(dead) / a z e r t y u i o p - + / q s d f g h j k l m / * / < w x c v b n . , : ;
shift # 1 2 3 4 5 6 7 8 9 0 " ¨(dead) / A Z E R T Y U I O P – ± / Q S D F G H J K L M \ ½ / > W X C V B N ? ! … =
```
Its AltGr layer holds many dead keys (breve, acute, grave, macron, caron, dot-above) and "mode" dead keys (Greek, currency) resolved by about 36 CLDR transforms.

**Belgian AZERTY** ([KBDBE](https://kbdlayout.info/KBDBE/))
```
base  E: ² & é " ' ( § è ! ç à ) -    D: a z e r t y u i o p ^(dead) $ µ    C: q s d f g h j k l m ù    B: < w x c v b n , ; : =
shift E: ³ 1 2 3 4 5 6 7 8 9 0 ° _    D: … ¨(dead) * £    C: … M %    B: > W X C V B N ? . / +
altgr E: · | @ # { [ ^ · · { } · ·    D: e→€, [ ], `(dead)    C: ´(dead)    B: \ … ~(dead)
```
**German QWERTZ** ([KBDGR](https://kbdlayout.info/KBDGR/))
```
base  E: ^(dead) 1 2 3 4 5 6 7 8 9 0 ß ´(dead)    D: q w e r t z u i o p ü + #    C: a s d f g h j k l ö ä    B: < y x c v b n m , . -
shift E: ° ! " § $ % & / ( ) = ? `(dead)          D: Q W E R T Z U I O P Ü * '    C: A S D F G H J K L Ö Ä    B: > Y X C V B N M ; : _
altgr E: · · ² ³ · · · { [ ] } \ ·    D: q→@, e→€, +→~    B: <→|, m→µ    (Shift+AltGr+ß → ẞ)
```
**Swiss German** ([KBDSG](https://kbdlayout.info/KBDSG/))
```
base  E: § 1 2 3 4 5 6 7 8 9 0 ' ^(dead)    D: q w e r t z u i o p ü ¨(dead) $    C: a s d f g h j k l ö ä    B: < y x c v b n m , . -
shift E: ° + " * ç % & / ( ) = ? `(dead)    D: … P è ! £    C: … L é à    B: > Y X C V B N M ; : _
altgr E: · ¦ @ # ° § ¬ | ¢ · · ´(dead) ~(dead)    D: e→€ [ ] }    C: {    B: \
```
**Spanish** ([KBDSP](https://kbdlayout.info/KBDSP/))
```
base  E: º 1 2 3 4 5 6 7 8 9 0 ' ¡    D: q … p `(dead) + ç    C: a … l ñ ´(dead)    B: < z x c v b n m , . -
shift E: ª ! " · $ % & / ( ) = ? ¿    D: … ^(dead) * Ç       C: … Ñ ¨(dead)       B: > Z … ; : _
altgr E: \ | @ # ~(dead) € ¬    D: e→€ [ ] }    C: {
```
**Italian** ([KBDIT](https://kbdlayout.info/KBDIT/)). It has no dead keys.
```
base  E: \ 1 2 3 4 5 6 7 8 9 0 ' ì    D: q … p è + ù    C: a … l ò à    B: < z … , . -
shift E: | ! " £ $ % & / ( ) = ? ^    D: … é * §       C: … ç °       B: > … ; : _
altgr e→€, 5→€, è→[, +→], ò→@, à→#;  shift+altgr è→{, +→}
```
**Portuguese (Brazil) ABNT2** ([KBDBR](https://kbdlayout.info/KBDBR/); [CLDR pt-t-k0-abnt2](https://github.com/unicode-org/cldr/blob/main/keyboards/3.0/pt-t-k0-abnt2.xml)). The final `/` sits on the extra key by right Shift.
```
base  E: ' 1 2 3 4 5 6 7 8 9 0 - =    D: q … p ´(dead) [ ]    C: a … l ç ~(dead)    B: \ z x c v b n m , . ; /
shift E: " ! @ # $ % ¨(dead) & * ( ) _ +    D: … `(dead) { }    C: … Ç ^(dead)    B: | Z … < > : ?
altgr E: ¹ ² ³ £ ¢ ¬ … §    D: q→/, w→?, e→°, [→ª, ]→º    B: c→₢
```
**Nordic** ([KBDSW](https://kbdlayout.info/KBDSW/), [KBDNO](https://kbdlayout.info/KBDNO/), [KBDDA](https://kbdlayout.info/KBDDA/))
```
sv/fi base  E: § 1 2 3 4 5 6 7 8 9 0 + ´(dead)    D: q … p å ¨(dead) '    C: a … l ö ä    B: < z … , . -
sv/fi shift E: ½ ! " # ¤ % & / ( ) = ? `(dead)    D: … Å ^(dead) *       C: … Ö Ä       B: > … ; : _
sv/fi altgr 2→@ 3→£ 4→$ 5→€ 7→{ 8→[ 9→] 0→} +→\ ¨→~(dead) <→| m→µ
nb          E: | 1…0 + \    D: … å ¨(dead) '    C: … ø æ
da          E: ½ 1…0 + ´(dead)    C: … æ ø
```
**Turkish Q** ([KBDTUQ](https://kbdlayout.info/KBDTUQ/)) and **Turkish F** ([KBDTUF](https://kbdlayout.info/KBDTUF/))
```
tr-Q base  E: " 1 2 3 4 5 6 7 8 9 0 * -    D: q w e r t y u ı o p ğ ü ,    C: a s d f g h j k l ş i    B: < z x c v b n m ö ç .
tr-Q shift E: é ! ' ^(dead) + % & / ( ) = ? _    D: Q W E R T Y U I O P Ğ Ü ;    C: A … L Ş İ    B: > Z … M Ö Ç :
tr-Q altgr E: < > £ # $ ½ · { [ ] } \ |    D: @ · € · ₺ · · i · · ¨(dead) ~(dead) `(dead)    C: æ ß · · · · · · · ´(dead) ·    B: | · · · · · · · · · ·
tr-Q shift-altgr: ı-key → İ, a → Æ
tr-F base  E: + 1 2 3 4 5 6 7 8 9 0 / -    D: f g ğ ı o d r n h p q w x    C: u i e a ü t k m l y ş    B: < j ö v c ç z s b . ,
tr-F shift E: * ! " ^(dead) $ % & ' ( ) = ? _    D: F G Ğ I O D R N H P Q W X    C: U İ E A Ü T K M L Y Ş    B: > J Ö V C Ç Z S B : ;
```
**Russian JCUKEN** ([KBDRU](https://kbdlayout.info/KBDRU/)). AltGr+8 gives ₽.
```
base  E: ё 1 2 3 4 5 6 7 8 9 0 - =    D: й ц у к е н г ш щ з х ъ \    C: ф ы в а п р о л д ж э    B: \ я ч с м и т ь б ю .
shift E: Ё ! " № ; % : ? * ( ) _ +    D: Й Ц У К Е Н Г Ш Щ З Х Ъ /    C: Ф Ы В А П Р О Л Д Ж Э    B: / Я Ч С М И Т Ь Б Ю ,
```
**Greek** ([KBDHE](https://kbdlayout.info/KBDHE/))
```
base  E: ` 1 … 0 - =    D: ; ς ε ρ τ υ θ ι ο π [ ] \    C: α σ δ φ γ η ξ κ λ ΄(dead) '    B: < ζ χ ψ ω β ν μ , . /
shift E: ~ ! @ # $ % ^ & * ( ) _ +    D: : ΅(dead) Ε Ρ Τ Υ Θ Ι Ο Π { } |    C: Α Σ Δ Φ Γ Η Ξ Κ Λ ¨(dead) "    B: > Ζ Χ Ψ Ω Β Ν Μ < > ?
altgr E: · · ² ³ £ § ¶ · ¤ ¦ ° ± ½    D: e→€ r→® y→¥ [→« ]→» \→¬    B: c→©
```
**Hebrew, Microsoft** ([KBDHEB](https://kbdlayout.info/KBDHEB/)). Shift gives Latin capitals, and brackets and parentheses are bidi-swapped. Ctrl+[ and Ctrl+] give LRM and RLM. AltGr: 4→₪, e→€, u→װ, h→ײ, j→ױ, -→ֿ.
```
base  E: ; 1 2 3 4 5 6 7 8 9 0 - =    D: / ' ק ר א ט ו ן ם פ ] [ \    C: ש ד ג כ ע י ח ל ך ף ,    B: \ ז ס ב ה נ מ צ ת ץ .
shift E: ~ ! @ # $ % ^ & * ) ( _ +    D: Q W E R T Y U I O P } { |    C: A … L : "    B: | Z X C V B N M > < ?
```
**Arabic 101** ([KBDA1](https://kbdlayout.info/KBDA1/)). The B-row key لا is two code points (U+0644 U+0627). Digits are European, and Ctrl+Shift+1..4 give ZWJ, ZWNJ, LRM and RLM. Recent Windows builds (dll 10.0.29667) add one AltGr entry, AltGr+S → U+20C1 SAUDI RIYAL SIGN; the catalog ignores it on purpose (fonts rarely have the glyph yet). Note that ذ is only on row E, which matters for the Compact form (PLAN.md 8.3).
```
base  E: ذ 1 2 3 4 5 6 7 8 9 0 - =    D: ض ص ث ق ف غ ع ه خ ح ج د \    C: ش س ي ب ل ا ت ن م ك ط    B: \ ئ ء ؤ ر لا ى ة و ز ظ
shift E: ◌ّ ! @ # $ % ^ & * ) ( _ +    D: ◌َ ◌ً ◌ُ ◌ٌ لإ إ ‘ ÷ × ؛ < > |    C: ◌ِ ◌ٍ ] [ لأ أ ـ ، / : "    B: | ~ ◌ْ } { لآ آ ’ , . ؟
```
**Persian ISIRI 9147** ([Wikipedia: ISIRI 9147](https://en.wikipedia.org/wiki/ISIRI_9147)). Shift+Space and Shift+B give ZWNJ, and AltGr+Space gives NBSP. AltGr+\ gives U+2010.
```
base  E: ZWJ ۱ ۲ ۳ ۴ ۵ ۶ ۷ ۸ ۹ ۰ - =    D: ض ص ث ق ف غ ع ه خ ح ج چ \    C: ش س ی ب ل ا ت ن م ک گ    B: \ ظ ط ز ر ذ د پ و . /
shift E: ÷ ! ٬ ٫ ﷼ ٪ × ، * ) ( ـ +    D: ◌ْ ◌ٌ ◌ٍ ◌ً ◌ُ ◌ِ ◌َ ◌ّ ] [ } { |    C: ؤ ئ ي إ أ آ ة » « : ؛    B: | ك ◌ٓ ژ ◌ٰ ZWNJ ◌ٔ ء > < ؟
altgr E: ~ ` @ # $ % ^ & • LRM RLM _ −    D: ° · € · · · · LRO RLO PDF LRE RLE ‐    C: · · ى · · ٱ · ﴾ ﴿ ; "    B: · · · · ◌ٖ ZWJ ◌ٕ … , ' ?
```
ISIRI defines no ISO (scan 56) key; its `\ |` is taken from KBDFAR. The last AltGr row-D character is U+2010 (hyphen). LRO, RLO, PDF, LRE and RLE are U+202D, U+202E, U+202C, U+202A and U+202B.

**Persian, Microsoft** (`fa-t-k0-windows`, [KBDFAR](https://kbdlayout.info/KBDFAR/)) is ISIRI with exactly these differences: Shift+\` gives ZWJ (as does the base layer), Shift+X gives ط, Shift+4 gives "ریال" (U+0631 U+06CC U+0627 U+0644), there is no AltGr layer, and Ctrl+Shift+1..4 give ZWJ, ZWNJ, LRM and RLM. Shift+Space also gives ZWNJ. As a consequence, ÷ and ◌ٓ cannot be typed on KBDFAR.

**Urdu** ([KBDURDU](https://kbdlayout.info/KBDURDU/)). The Ctrl+Shift layer holds the harakat and أ إ.
```
base  E: ` 1 … 0 - =    D: ط ص ھ د ٹ پ ت ب ج ح ] [ \    C: م و ر ن ل ہ ا ک ی ؛ '    B: \ ق ف ے س ش غ ع ، ۔ /
shift E: ~ ! @ # $ ٪ ^ ۖ ٭ ) ( _ +    D: ظ ض ذ ڈ ث ◌ّ ۃ ـ چ خ } { |    C: ژ ز ڑ ں ۂ ء آ گ ي : "    B: | ZWJ ZWNJ ۓ LRM ؤ ئ RLM > < ؟
```
**Hindi InScript** ([KBDINHIN](https://kbdlayout.info/KBDINHIN/)). AltGr on row E gives Devanagari digits, and Ctrl+Shift+4 gives ₹.
```
base  E: · 1 2 3 4 5 6 7 8 9 0 - ृ    D: ौ ै ा ी ू ब ह ग द ज ड ़ ॉ    C: ो े ् ि ु प र क त च ट    B: ॉ · ं म न व ल स , . य
shift E: · ऍ ॅ ्र र् ज्ञ त्र क्ष श्र ( ) ः ऋ    D: औ ऐ आ ई ऊ भ ङ घ ध झ ढ ञ ऑ    C: ओ ए अ इ उ फ ऱ ख थ छ ठ    B: ऑ · ँ ण · · ळ श ष । य़
```
**Thai Kedmanee** ([KBDTH0](https://kbdlayout.info/KBDTH0/))
```
base  E: _ ๅ / - ภ ถ ุ ึ ค ต จ ข ช    D: ๆ ไ ำ พ ะ ั ี ร น ย บ ล ฃ    C: ฟ ห ก ด เ ้ ่ า ส ว ง    B: ฃ ผ ป แ อ ิ ื ท ม ใ ฝ
shift E: % + ๑ ๒ ๓ ๔ ู ฿ ๕ ๖ ๗ ๘ ๙    D: ๐ " ฎ ฑ ธ ํ ๊ ณ ฯ ญ ฐ , ฅ    C: ฤ ฆ ฏ โ ฌ ็ ๋ ษ ศ ซ .    B: ฅ ( ) ฉ ฮ ฺ ์ ? ฒ ฬ ฦ
```
**Dvorak** ([KBDDV](https://kbdlayout.info/KBDDV/)), **Colemak** and **Colemak-DH** ([xkeyboard-config symbols/us](https://gitlab.freedesktop.org/xkeyboard-config/xkeyboard-config/-/blob/master/symbols/us)). Shift layers are the uppercase letters plus US symbols, and Dvorak's shift D row is `" < > P Y F G C R L ? + |`.
```
dvorak   E: ` 1 … 0 [ ]    D: ' , . p y f g c r l / = \    C: a o e u i d h t n s -    B: ; q j k x b m w v z
colemak  D: q w f p g j l u y ; [ ]    C: a r s t d h n e i o '    B: z x c v b k m , . /
colemakdh D: q w f p b j l u y ; [ ]   C: a r s t g m n e i o '    B: x c d v z k h , . /   (ISO: z also on the <> key)
```
**Korean Dubeolsik** ([Wikipedia: Dubeolsik](https://en.wikipedia.org/wiki/Keyboard_layout#Dubeolsik)). Shift gives ㅃ ㅉ ㄸ ㄲ ㅆ on Q–T and ㅒ ㅖ on O and P. Jamo compose algorithmically into syllables.
```
Q-row: ㅂ ㅈ ㄷ ㄱ ㅅ ㅛ ㅕ ㅑ ㅐ ㅔ    A-row: ㅁ ㄴ ㅇ ㄹ ㅎ ㅗ ㅓ ㅏ ㅣ    Z-row: ㅋ ㅌ ㅊ ㅍ ㅠ ㅜ ㅡ
```
**Japanese 12-key flick** ([CLDR ja-Hira-t-k0-flicks](https://github.com/unicode-org/cldr/blob/main/keyboards/3.0/ja-Hira-t-k0-flicks.xml)). Base rows: `あ か さ` / `た な は` / `ま や ら` / `sym わ 。 num` / `space`. The num layer is `1 2 3` / `4 5 6` / `7 8 9` / `, 0 .`. On each key, a tap gives the key's own kana; flicking west, east, south-west and south gives the i, u, e and o columns (あ: い う え お). Common phone UIs place these differently, so make the flick map data, not code.

Several gaps remain unverified:
- Hebrew SI-1452 punctuation differences from Microsoft's layout
- Arabic 102 and Maghreb AZERTY-Arabic variants
- JIS kana-on-key assignments
- the official ISIRI and KS X 5002 texts, which were not read directly

Treat these as follow-up checks before the P1 and P2 tiers ship.

