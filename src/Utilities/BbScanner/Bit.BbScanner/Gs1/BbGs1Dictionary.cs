using System.Collections.Frozen;

namespace Bit.BbScanner;

/// <summary>
/// The GS1 Application Identifiers BbScanner knows, with the format each one's data field has.
/// </summary>
/// <remarks>
/// <para>
/// The entries follow the GS1 General Specifications (section 3, "GS1 Application Identifier definitions") in the
/// notation of the GS1 Barcode Syntax Dictionary: a field is one or more components, each a character set - <c>N</c>
/// digits, <c>X</c> GS1 AI encodable character set 82, <c>Y</c> character set 39, <c>Z</c> character set 64 - and a
/// length, fixed (<c>N6</c>) or variable up to a maximum (<c>X..20</c>), optionally followed by a check
/// (<c>N14,csum</c>). A component in brackets may be left off the end of the field.
/// </para>
/// <para>
/// The checks applied are the ones that need no external list: GS1 modulo 10 check digits, the calendar
/// (<c>yymmdd</c>, <c>yymmd0</c>, where a day of 00 means the month's last day), times of day, and a handful of fixed
/// values. Not applied: country and currency codes against ISO 3166 and ISO 4217 (they are checked to be digits or
/// capitals only), the alphanumeric check character pair of AIs 8013 and 8014, IBAN check digits, coordinate ranges,
/// importer indices, package and media type lists, and percent-encoding.
/// </para>
/// </remarks>
internal static class BbGs1Dictionary
{
    /// <summary>
    /// The two digit AI prefixes whose element strings have a predefined length, which therefore need no FNC1 after
    /// them (GS1 General Specifications, figure 7.8.4-2), with the length of the whole element string, AI included.
    /// </summary>
    internal static readonly FrozenDictionary<string, int> PredefinedLengths = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["00"] = 20, ["01"] = 16, ["02"] = 16, ["03"] = 16, ["04"] = 18,
        ["11"] = 8, ["12"] = 8, ["13"] = 8, ["14"] = 8, ["15"] = 8, ["16"] = 8, ["17"] = 8, ["18"] = 8, ["19"] = 8,
        ["20"] = 4,
        ["31"] = 10, ["32"] = 10, ["33"] = 10, ["34"] = 10, ["35"] = 10, ["36"] = 10,
        ["41"] = 16,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    internal static readonly FrozenDictionary<string, BbGs1AiDefinition> Definitions = Build();

    /// <summary>Finds the AI at the start of <paramref name="text"/>: AIs are prefix free, so at most one matches.</summary>
    internal static BbGs1AiDefinition? Match(ReadOnlySpan<char> text)
    {
        var lookup = Definitions.GetAlternateLookup<ReadOnlySpan<char>>();
        for (var length = 2; length <= 4 && length <= text.Length; length++)
        {
            if (lookup.TryGetValue(text[..length], out var definition))
            {
                return definition;
            }
        }

        return null;
    }

    private static FrozenDictionary<string, BbGs1AiDefinition> Build()
    {
        var table = new Dictionary<string, BbGs1AiDefinition>(StringComparer.Ordinal);

        void Add(string ai, string title, string format, BbGs1ValueKind kind = BbGs1ValueKind.Text)
        {
            var components = ParseFormat(format);
            table.Add(ai, new BbGs1AiDefinition(ai, title, components, kind, DecimalPlaces(ai, kind)));
        }

        // A run of AIs that differ only in their last digit, such as 3100 to 3105, where that digit is the position
        // of the implied decimal point.
        void AddRange(string first, string last, string title, string format, BbGs1ValueKind kind = BbGs1ValueKind.Text)
        {
            var prefix = first[..^1];
            for (var digit = first[^1]; digit <= last[^1]; digit++)
            {
                Add(prefix + digit, title, format, kind);
            }
        }

        // Identification of a logistic unit, of a trade item, and of the trade items a unit contains.
        Add("00", "SSCC", "N18,csum");
        Add("01", "GTIN", "N14,csum");
        Add("02", "CONTENT", "N14,csum");
        Add("03", "MTO GTIN", "N14,csum");

        // Batch, dates and variants.
        Add("10", "BATCH/LOT", "X..20");
        Add("11", "PROD DATE", "N6,yymmd0", BbGs1ValueKind.Date);
        Add("12", "DUE DATE", "N6,yymmd0", BbGs1ValueKind.Date);
        Add("13", "PACK DATE", "N6,yymmd0", BbGs1ValueKind.Date);
        Add("15", "BEST BEFORE or BEST BY", "N6,yymmd0", BbGs1ValueKind.Date);
        Add("16", "SELL BY", "N6,yymmd0", BbGs1ValueKind.Date);
        Add("17", "USE BY or EXPIRY", "N6,yymmd0", BbGs1ValueKind.Date);
        Add("20", "VARIANT", "N2");
        Add("21", "SERIAL", "X..20");
        Add("22", "CPV", "X..20");
        Add("235", "TPX", "X..28");
        Add("240", "ADDITIONAL ID", "X..30");
        Add("241", "CUST. PART No.", "X..30");
        Add("242", "MTO VARIANT", "N..6");
        Add("243", "PCN", "X..20");
        Add("250", "SECONDARY SERIAL", "X..30");
        Add("251", "REF. TO SOURCE", "X..30");
        Add("253", "GDTI", "N13,csum [X..17]");
        Add("254", "GLN EXTENSION COMPONENT", "X..20");
        Add("255", "GCN", "N13,csum [N..12]");
        Add("30", "VAR. COUNT", "N..8", BbGs1ValueKind.Integer);
        Add("37", "COUNT", "N..8", BbGs1ValueKind.Integer);

        // Trade and logistic measures: six digits, the AI's last digit giving the decimal places.
        (string Prefix, string Title)[] measures =
        [
            ("310", "NET WEIGHT (kg)"), ("311", "LENGTH (m)"), ("312", "WIDTH (m)"), ("313", "HEIGHT (m)"),
            ("314", "AREA (m²)"), ("315", "NET VOLUME (l)"), ("316", "NET VOLUME (m³)"),
            ("320", "NET WEIGHT (lb)"), ("321", "LENGTH (in)"), ("322", "LENGTH (ft)"), ("323", "LENGTH (yd)"),
            ("324", "WIDTH (in)"), ("325", "WIDTH (ft)"), ("326", "WIDTH (yd)"), ("327", "HEIGHT (in)"),
            ("328", "HEIGHT (ft)"), ("329", "HEIGHT (yd)"),
            ("330", "GROSS WEIGHT (kg)"), ("331", "LENGTH (m), log"), ("332", "WIDTH (m), log"),
            ("333", "HEIGHT (m), log"), ("334", "AREA (m²), log"), ("335", "VOLUME (l), log"),
            ("336", "VOLUME (m³), log"), ("337", "KG PER m²"),
            ("340", "GROSS WEIGHT (lb)"), ("341", "LENGTH (in), log"), ("342", "LENGTH (ft), log"),
            ("343", "LENGTH (yd), log"), ("344", "WIDTH (in), log"), ("345", "WIDTH (ft), log"),
            ("346", "WIDTH (yd), log"), ("347", "HEIGHT (in), log"), ("348", "HEIGHT (ft), log"),
            ("349", "HEIGHT (yd), log"),
            ("350", "AREA (in²)"), ("351", "AREA (ft²)"), ("352", "AREA (yd²)"), ("353", "AREA (in²), log"),
            ("354", "AREA (ft²), log"), ("355", "AREA (yd²), log"), ("356", "NET WEIGHT (troy oz)"),
            ("357", "NET VOLUME (oz)"),
            ("360", "NET VOLUME (qt)"), ("361", "NET VOLUME (gal.)"), ("362", "VOLUME (qt), log"),
            ("363", "VOLUME (gal.), log"), ("364", "VOLUME (in³)"), ("365", "VOLUME (ft³)"),
            ("366", "VOLUME (yd³)"), ("367", "VOLUME (in³), log"), ("368", "VOLUME (ft³), log"),
            ("369", "VOLUME (yd³), log"),
        ];
        foreach (var (prefix, title) in measures)
        {
            AddRange(prefix + "0", prefix + "5", title, "N6", BbGs1ValueKind.Decimal);
        }

        // Amounts and prices, in local currency or with an ISO 4217 numeric currency code ahead of them.
        AddRange("3900", "3909", "AMOUNT", "N..15", BbGs1ValueKind.Decimal);
        AddRange("3910", "3919", "AMOUNT", "N3,iso4217 N..15", BbGs1ValueKind.CurrencyDecimal);
        AddRange("3920", "3929", "PRICE", "N..15", BbGs1ValueKind.Decimal);
        AddRange("3930", "3939", "PRICE", "N3,iso4217 N..15", BbGs1ValueKind.CurrencyDecimal);
        AddRange("3940", "3943", "PRCNT OFF", "N4", BbGs1ValueKind.Decimal);
        AddRange("3950", "3955", "PRICE/UoM", "N6", BbGs1ValueKind.Decimal);

        // Orders, consignments, shipments and locations.
        Add("400", "ORDER NUMBER", "X..30");
        Add("401", "GINC", "X..30");
        Add("402", "GSIN", "N17,csum");
        Add("403", "ROUTE", "X..30");
        Add("410", "SHIP TO LOC", "N13,csum");
        Add("411", "BILL TO", "N13,csum");
        Add("412", "PURCHASE FROM", "N13,csum");
        Add("413", "SHIP FOR LOC", "N13,csum");
        Add("414", "LOC No.", "N13,csum");
        Add("415", "PAY TO", "N13,csum");
        Add("416", "PROD/SERV LOC", "N13,csum");
        Add("417", "PARTY", "N13,csum");
        Add("420", "SHIP TO POST", "X..20");
        Add("421", "SHIP TO POST", "N3,iso3166 X..9");
        Add("422", "ORIGIN", "N3,iso3166");
        Add("423", "COUNTRY - INITIAL PROCESS", "N3..15,iso3166list");
        Add("424", "COUNTRY - PROCESS", "N3,iso3166");
        Add("425", "COUNTRY - DISASSEMBLY", "N3..15,iso3166list");
        Add("426", "COUNTRY - FULL PROCESS", "N3,iso3166");
        Add("427", "ORIGIN SUBDIVISION", "X..3");

        // Transport process information: addresses, phone numbers, delivery windows and temperatures.
        Add("4300", "SHIP TO COMP", "X..35");
        Add("4301", "SHIP TO NAME", "X..35");
        Add("4302", "SHIP TO ADD1", "X..70");
        Add("4303", "SHIP TO ADD2", "X..70");
        Add("4304", "SHIP TO SUB", "X..70");
        Add("4305", "SHIP TO LOC", "X..70");
        Add("4306", "SHIP TO REG", "X..70");
        Add("4307", "SHIP TO COUNTRY", "X2,iso3166alpha2");
        Add("4308", "SHIP TO PHONE", "X..30");
        Add("4309", "SHIP TO GEO", "N20");
        Add("4310", "RTN TO COMP", "X..35");
        Add("4311", "RTN TO NAME", "X..35");
        Add("4312", "RTN TO ADD1", "X..70");
        Add("4313", "RTN TO ADD2", "X..70");
        Add("4314", "RTN TO SUB", "X..70");
        Add("4315", "RTN TO LOC", "X..70");
        Add("4316", "RTN TO REG", "X..70");
        Add("4317", "RTN TO COUNTRY", "X2,iso3166alpha2");
        Add("4318", "RTN TO POST", "X..20");
        Add("4319", "RTN TO PHONE", "X..30");
        Add("4320", "SRV DESCRIPTION", "X..35");
        Add("4321", "DANGEROUS GOODS", "N1,yesno");
        Add("4322", "AUTH LEAVE", "N1,yesno");
        Add("4323", "SIG REQUIRED", "N1,yesno");
        Add("4324", "NBEF DEL DT", "N6,yymmd0 N4,hhmi", BbGs1ValueKind.Date);
        Add("4325", "NAFT DEL DT", "N6,yymmd0 N4,hhmi", BbGs1ValueKind.Date);
        Add("4326", "REL DATE", "N6,yymmdd", BbGs1ValueKind.Date);
        Add("4330", "MAX TEMP F", "N6 [X1,hyphen]");
        Add("4331", "MAX TEMP C", "N6 [X1,hyphen]");
        Add("4332", "MIN TEMP F", "N6 [X1,hyphen]");
        Add("4333", "MIN TEMP C", "N6 [X1,hyphen]");

        // Product attributes: healthcare, fishery and the processing chain.
        Add("7001", "NSN", "N13");
        Add("7002", "MEAT CUT", "X..30");
        Add("7003", "EXPIRY TIME", "N10,yymmddhhmi", BbGs1ValueKind.Date);
        Add("7004", "ACTIVE POTENCY", "N..4");
        Add("7005", "CATCH AREA", "X..12");
        Add("7006", "FIRST FREEZE DATE", "N6,yymmdd", BbGs1ValueKind.Date);
        Add("7007", "HARVEST DATE", "N6,yymmdd [N6,yymmdd]", BbGs1ValueKind.Date);
        Add("7008", "AQUATIC SPECIES", "X..3");
        Add("7009", "FISHING GEAR TYPE", "X..10");
        Add("7010", "PROD METHOD", "X..2");
        Add("7011", "TEST BY DATE", "N6,yymmdd [N4,hhmi]", BbGs1ValueKind.Date);
        Add("7020", "REFURB LOT", "X..20");
        Add("7021", "FUNC STAT", "X..20");
        Add("7022", "REV STAT", "X..20");
        Add("7023", "GIAI - ASSEMBLY", "X..30");
        AddRange("7030", "7039", "PROCESSOR # s", "N3,iso3166 X..27");
        Add("7040", "UIC+EXT", "N1 X1 X1 X1");
        Add("7041", "UFRGT UNIT TYPE", "X..4");
        Add("710", "NHRN PZN", "X..20");
        Add("711", "NHRN CIP", "X..20");
        Add("712", "NHRN CN", "X..20");
        Add("713", "NHRN DRN", "X..20");
        Add("714", "NHRN AIM", "X..20");
        Add("715", "NHRN NDC", "X..20");
        Add("716", "NHRN AIC", "X..20");
        Add("717", "NHRN SRN", "X..20");
        AddRange("7230", "7239", "CERT # s", "X2 X..28");
        Add("7240", "PROTOCOL", "X..20");
        Add("7241", "AIDC MEDIA TYPE", "N2");
        Add("7242", "VCN", "X..25");
        Add("7250", "DOB", "N8,yyyymmdd", BbGs1ValueKind.Date);
        Add("7251", "DOB TIME", "N12,yyyymmddhhmi", BbGs1ValueKind.Date);
        Add("7252", "BIO SEX", "N1,iso5218");
        Add("7253", "FAMILY NAME", "X..40");
        Add("7254", "GIVEN NAME", "X..40");
        Add("7255", "SUFFIX", "X..10");
        Add("7256", "FULL NAME", "X..90");
        Add("7257", "PERSON ADDR", "X..70");
        Add("7258", "BIRTH SEQUENCE", "N1 X1,slash N1");
        Add("7259", "BABY", "X..40");

        // Identification of assets, services, components and coupons.
        Add("8001", "DIMENSIONS", "N14");
        Add("8002", "CMT No.", "X..20");
        Add("8003", "GRAI", "N1,zero N13,csum [X..16]");
        Add("8004", "GIAI", "X..30");
        Add("8005", "PRICE PER UNIT", "N6");
        Add("8006", "ITIP", "N14,csum N4,pieceoftotal");
        Add("8007", "IBAN", "X..34");
        Add("8008", "PROD TIME", "N8,yymmddhh [N..4,mmoptss]", BbGs1ValueKind.Date);
        Add("8009", "OPTSEN", "X..50");
        Add("8010", "CPID", "Y..30");
        Add("8011", "CPID SERIAL", "N..12,nozeroprefix");
        Add("8012", "VERSION", "X..20");
        Add("8013", "GMN", "X..25");
        Add("8014", "MUDI", "X..25");
        Add("8017", "GSRN - PROVIDER", "N18,csum");
        Add("8018", "GSRN - RECIPIENT", "N18,csum");
        Add("8019", "SRIN", "N..10");
        Add("8020", "REF No.", "X..25");
        Add("8026", "ITIP CONTENT", "N14,csum N4,pieceoftotal");
        Add("8030", "DIGSIG", "Z..90");
        Add("8040", "IMEI", "N15");
        Add("8041", "IMEI2", "N15");
        Add("8042", "ESIM", "N32");
        Add("8043", "PSIM", "N18 [N..2]");
        Add("8110", "COUPON", "X..70");
        Add("8111", "POINTS", "N4");
        Add("8112", "COUPON", "X..70");
        Add("8200", "PRODUCT URL", "X..70");

        // Information agreed between trading partners, and company internal information.
        Add("90", "INTERNAL", "X..30");
        for (var ai = 91; ai <= 99; ai++)
        {
            Add(ai.ToString(System.Globalization.CultureInfo.InvariantCulture), "INTERNAL", "X..90");
        }

        return table.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static int? DecimalPlaces(string ai, BbGs1ValueKind kind) =>
        kind is BbGs1ValueKind.Decimal or BbGs1ValueKind.CurrencyDecimal ? ai[^1] - '0' : null;

    private static BbGs1Component[] ParseFormat(string format)
    {
        var parts = format.Split(' ');
        var components = new BbGs1Component[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var optional = part.StartsWith('[');
            if (optional)
            {
                part = part[1..^1];
            }

            var comma = part.IndexOf(',', StringComparison.Ordinal);
            var check = comma < 0 ? null : part[(comma + 1)..];
            var shape = comma < 0 ? part : part[..comma];

            var charset = shape[0] switch
            {
                'N' => BbGs1Charset.Numeric,
                'X' => BbGs1Charset.Cset82,
                'Y' => BbGs1Charset.Cset39,
                'Z' => BbGs1Charset.Cset64,
                _ => throw new InvalidOperationException($"Unknown character set in '{format}'."),
            };

            int min, max;
            var lengths = shape[1..];
            var range = lengths.IndexOf("..", StringComparison.Ordinal);
            if (range < 0)
            {
                min = max = int.Parse(lengths, System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                min = range == 0 ? 1 : int.Parse(lengths[..range], System.Globalization.CultureInfo.InvariantCulture);
                max = int.Parse(lengths[(range + 2)..], System.Globalization.CultureInfo.InvariantCulture);
            }

            components[i] = new BbGs1Component(charset, min, max, optional, check);
        }

        return components;
    }
}

/// <summary>What an element's value means, for the typed accessors of <see cref="BbGs1Element"/>.</summary>
internal enum BbGs1ValueKind
{
    Text,
    Integer,
    Decimal,
    CurrencyDecimal,
    Date,
}

internal enum BbGs1Charset
{
    Numeric,
    Cset82,
    Cset39,
    Cset64,
}

internal sealed record BbGs1Component(BbGs1Charset Charset, int MinLength, int MaxLength, bool Optional, string? Check)
{
    public bool IsFixed => MinLength == MaxLength;
}

internal sealed record BbGs1AiDefinition(
    string Ai,
    string Title,
    BbGs1Component[] Components,
    BbGs1ValueKind Kind,
    int? DecimalPlaces)
{
    /// <summary>The length of the data field when every component, optional ones included, is fixed and present.</summary>
    public int? FixedLength
    {
        get
        {
            var length = 0;
            foreach (var component in Components)
            {
                if (!component.IsFixed || component.Optional)
                {
                    return null;
                }

                length += component.MaxLength;
            }

            return length;
        }
    }
}
