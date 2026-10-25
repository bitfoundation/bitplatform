// Generates UnicodeTables.cs - the Unicode character data the text shaper needs - from the Unicode Character Database.
//
//   node generate-unicode-tables.mjs <ucd directory>
//
// The directory holds these files from https://www.unicode.org/Public/<version>/ucd/ (the extracted/ ones flattened
// into it): DerivedBidiClass.txt, DerivedJoiningType.txt, BidiMirroring.txt, BidiBrackets.txt, Scripts.txt and
// DerivedCoreProperties.txt. Every table is a sorted list of runs, (first code point << bits) | value, each run lasting
// until the next one starts, so a lookup is one binary search over constant data.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ucd = process.argv[2];
if (!ucd) throw new Error('usage: node generate-unicode-tables.mjs <ucd directory>');
const read = name => fs.readFileSync(path.join(ucd, name), 'utf8');
const MAX = 0x10FFFF;

/** Lines of "range ; value", honouring the file's @missing defaults, as a dense array over every code point. */
function dense(text, parse, fallback) {
    const values = new Array(MAX + 1).fill(fallback);
    const apply = (range, value) => {
        const [a, b] = range.split('..').map(s => parseInt(s, 16));
        for (let c = a; c <= (b ?? a); c++) values[c] = value;
    };
    for (const line of text.split('\n')) {
        const missing = /^#\s*@missing:\s*([0-9A-F.]+)\s*;\s*([^#;\s]+)/.exec(line);
        if (missing) { const v = parse(missing[2]); if (v !== undefined) apply(missing[1], v); continue; }
        const body = line.split('#')[0].trim();
        if (!body) continue;
        const [range, value] = body.split(';').map(s => s.trim());
        const v = parse(value);
        if (v !== undefined) apply(range, v);
    }
    return values;
}

/** Run-length encodes a dense array into (start << bits) | value. */
function runs(values, bits) {
    const out = [];
    let previous = -1;
    for (let c = 0; c <= MAX; c++) {
        if (values[c] !== previous) { out.push((c * (1 << bits)) + values[c]); previous = values[c]; }
    }
    return out;
}

const header = fs.readFileSync(path.join(ucd, 'DerivedBidiClass.txt'), 'utf8').split('\n')[0].replace(/^#\s*/, '');
const version = /-(\d+\.\d+\.\d+)\.txt/.exec(header)?.[1] ?? 'unknown';

const bidiClasses = ['L', 'R', 'AL', 'EN', 'ES', 'ET', 'AN', 'CS', 'NSM', 'BN', 'B', 'S', 'WS', 'ON', 'LRE', 'LRO', 'RLE', 'RLO', 'PDF', 'LRI', 'RLI', 'FSI', 'PDI'];
const bidiNames = { L: 'Left_To_Right', R: 'Right_To_Left', AL: 'Arabic_Letter', EN: 'European_Number', ES: 'European_Separator', ET: 'European_Terminator', AN: 'Arabic_Number', CS: 'Common_Separator', NSM: 'Nonspacing_Mark', BN: 'Boundary_Neutral', B: 'Paragraph_Separator', S: 'Segment_Separator', WS: 'White_Space', ON: 'Other_Neutral', LRE: 'Left_To_Right_Embedding', LRO: 'Left_To_Right_Override', RLE: 'Right_To_Left_Embedding', RLO: 'Right_To_Left_Override', PDF: 'Pop_Directional_Format', LRI: 'Left_To_Right_Isolate', RLI: 'Right_To_Left_Isolate', FSI: 'First_Strong_Isolate', PDI: 'Pop_Directional_Isolate' };
const bidiIndex = v => { const i = bidiClasses.indexOf(v); if (i >= 0) return i; for (const [k, n] of Object.entries(bidiNames)) if (n === v) return bidiClasses.indexOf(k); return undefined; };
const bidi = runs(dense(read('DerivedBidiClass.txt'), bidiIndex, 0), 5);

const joiningTypes = ['U', 'R', 'L', 'D', 'C', 'T'];
const joiningNames = { Non_Joining: 'U', Right_Joining: 'R', Left_Joining: 'L', Dual_Joining: 'D', Join_Causing: 'C', Transparent: 'T' };
const joiningIndex = v => { const k = joiningTypes.includes(v) ? v : joiningNames[v]; return k === undefined ? undefined : joiningTypes.indexOf(k); };
const joining = runs(dense(read('DerivedJoiningType.txt'), joiningIndex, 0), 3);

// Only the scripts the shaper picks OpenType features by; everything else is Other.
const scripts = ['Common', 'Inherited', 'Latin', 'Greek', 'Cyrillic', 'Arabic', 'Hebrew', 'Other'];
const scriptIndex = v => { const i = scripts.indexOf(v); return i >= 0 ? i : 7; };
const script = runs(dense(read('Scripts.txt'), scriptIndex, 0), 3);

const ignorable = runs(dense(read('DerivedCoreProperties.txt'), v => (v === 'Default_Ignorable_Code_Point' ? 1 : undefined), 0), 1);

const mirrors = [];
for (const line of read('BidiMirroring.txt').split('\n')) {
    const body = line.split('#')[0].trim();
    if (!body) continue;
    const [a, b] = body.split(';').map(s => parseInt(s.trim(), 16));
    mirrors.push([a, b]);
}
mirrors.sort((x, y) => x[0] - y[0]);

const brackets = [];
for (const line of read('BidiBrackets.txt').split('\n')) {
    const body = line.split('#')[0].trim();
    if (!body) continue;
    const [a, b, t] = body.split(';').map(s => s.trim());
    brackets.push([parseInt(a, 16), parseInt(b, 16), t === 'o' ? 1 : 2]);
}
brackets.sort((x, y) => x[0] - y[0]);

const hex = n => '0x' + n.toString(16).toUpperCase();
const list = (values, perLine = 10) => {
    const lines = [];
    for (let i = 0; i < values.length; i += perLine) lines.push('        ' + values.slice(i, i + perLine).map(hex).join(', ') + ',');
    return lines.join('\n');
};

const out = `// <auto-generated>
// Generated by generate-unicode-tables.mjs from the Unicode Character Database ${version}. Do not edit by hand: run
// the script against a newer database instead.
// </auto-generated>

namespace Bit.Bmage.Text.Unicode;

/// <summary>Unicode ${version} character data, as runs of (first code point &lt;&lt; bits) | value.</summary>
internal static partial class UnicodeTables
{
    /// <summary>The Unicode version the tables were generated from.</summary>
    public const string Version = "${version}";

    /// <summary>Bidi_Class, 5 bits, in <see cref="BidiClass"/> order.</summary>
    public static ReadOnlySpan<uint> BidiClassRuns =>
    [
${list(bidi)}
    ];

    /// <summary>Joining_Type, 3 bits, in <see cref="JoiningType"/> order.</summary>
    public static ReadOnlySpan<uint> JoiningTypeRuns =>
    [
${list(joining)}
    ];

    /// <summary>The scripts the shaper selects OpenType features by, 3 bits, in <see cref="UnicodeScript"/> order.</summary>
    public static ReadOnlySpan<uint> ScriptRuns =>
    [
${list(script)}
    ];

    /// <summary>Default_Ignorable_Code_Point, 1 bit.</summary>
    public static ReadOnlySpan<uint> IgnorableRuns =>
    [
${list(ignorable)}
    ];

    /// <summary>Bidi_Mirroring_Glyph: pairs of a code point and its mirror, sorted by the first.</summary>
    public static ReadOnlySpan<uint> MirrorPairs =>
    [
${list(mirrors.flat())}
    ];

    /// <summary>Bidi_Paired_Bracket: triples of a code point, its pair and its type (1 open, 2 close), sorted by the first.</summary>
    public static ReadOnlySpan<uint> BracketTriples =>
    [
${list(brackets.flat(), 9)}
    ];
}
`;
const target = path.join(path.dirname(fileURLToPath(import.meta.url)), 'UnicodeTables.cs');
fs.writeFileSync(target, out.replace(/\n/g, '\r\n'));
console.log(`wrote ${target}: bidi ${bidi.length}, joining ${joining.length}, script ${script.length}, ignorable ${ignorable.length}, mirrors ${mirrors.length}, brackets ${brackets.length}`);
