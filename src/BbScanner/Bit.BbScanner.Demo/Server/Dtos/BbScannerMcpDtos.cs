namespace Bit.BbScanner.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBbScannerGuideSection (and the bbscanner://guide resources); this record is how the catalog,
/// the search index and the completions describe a section to each other.
/// </summary>
public record BbScannerGuideSectionDto
{
    /// <summary>The heading text, e.g. "Using the decoder without Blazor".</summary>
    public required string Heading { get; init; }

    /// <summary>
    /// What to pass to GetBbScannerGuideSection for exactly this section: the heading itself, or - for the
    /// few headings the README uses twice - "Parent > Heading".
    /// </summary>
    public required string Key { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 or 4 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The heading this one sits under, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}

/// <summary>A public type of the Bit.BbScanner assembly.</summary>
public record BbScannerApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method or enum value) of a public Bit.BbScanner type.</summary>
public record BbScannerApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, Method, Event or EnumValue. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(ReadOnlySpan&lt;byte&gt; pixels, int width, int height, BbPixelFormat format, bool suppressDuplicates = true)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.BbScanner type.</summary>
public record BbScannerApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BbScannerApiMemberDto[] Members { get; init; }
}

/// <summary>A source file this server can hand out, retrievable through GetBbScannerSourceFile.</summary>
public record BbScannerSourceFileDto
{
    /// <summary>The path to pass to GetBbScannerSourceFile, e.g. "Demo/Client/Pages/Scan.razor".</summary>
    public required string Path { get; init; }

    /// <summary>Page (a working demo page), Hosting (the wiring of the app), Library (a shipped file) or Shared.</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page title, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}


/// <summary>One result of a search across everything this MCP server knows about Bit.BbScanner.</summary>
public record BbScannerSearchHitDto
{
    /// <summary>What was found: "Guide section", "API component", "API parameter", "Symbology", "CSS class", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or group.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBbScanner answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BbScannerSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BbScannerSearchHitDto[] Hits { get; init; }

    /// <summary>True when the ranking was cut short at the requested limit, so a narrower query would show more.</summary>
    public bool HasMore { get; init; }

    /// <summary>
    /// The words the query was actually searched by - punctuation, duplicates and filler words
    /// removed. Present only when nothing matched, which is the one time it explains something the
    /// caller could not work out from the query it just sent.
    /// </summary>
    public string[]? Terms { get; init; }

    /// <summary>Set only when Hits is empty: the reason, and the tool to fall back to.</summary>
    public string? Message { get; init; }

    /// <summary>Titles that nearly matched, when nothing matched outright - a query worth retrying with.</summary>
    public string[]? DidYouMean { get; init; }
}

/// <summary>
/// One symbology the library reads, described from the library itself: its flag in BbBarcodeFormat, the
/// documentation of that flag, whether the pipeline actually creates its reader, and the options that steer it
/// with the defaults a freshly created BbScannerOptions has.
/// </summary>
public record BbScannerFormatDto
{
    /// <summary>The BbBarcodeFormat member, e.g. "QrCode".</summary>
    public required string Name { get; init; }

    /// <summary>The symbology's usual name, e.g. "QR Code".</summary>
    public required string Title { get; init; }

    /// <summary>Matrix (two dimensional) or Linear (one dimensional).</summary>
    public required string Family { get; init; }

    /// <summary>The flag's numeric value, so a combination such as QrCode | Ean13 can be read back.</summary>
    public required int Value { get; init; }

    /// <summary>The XML documentation of the flag: the standard it implements.</summary>
    public string? Summary { get; init; }

    /// <summary>"Supported" or "Partial", as the guide's symbology table states it.</summary>
    public string? Status { get; init; }

    /// <summary>What of the symbology is read, as the guide's symbology table states it.</summary>
    public string? Notes { get; init; }

    /// <summary>True when a new BbScannerOptions has this flag in its Formats.</summary>
    public required bool EnabledByDefault { get; init; }

    /// <summary>
    /// True when BbBarcodeDecoder creates a reader for it. False only for PDF417 while the registered symbol
    /// character table is not the specification one (BbPdf417SymbolTable.Current.IsSpecificationTable).
    /// </summary>
    public required bool Active { get; init; }

    /// <summary>The BbScannerOptions members that change how this symbology is read, with their defaults.</summary>
    public required BbScannerOptionDto[] Options { get; init; }

    /// <summary>The payload the decode lab encodes when it is asked for this symbology without one.</summary>
    public required string SampleText { get; init; }
}

/// <summary>One BbScannerOptions member and the value a freshly created instance has.</summary>
public record BbScannerOptionDto
{
    public required string Name { get; init; }

    public required string Type { get; init; }

    public string? Default { get; init; }

    /// <summary>Why the member matters to the symbology it is listed under.</summary>
    public required string Effect { get; init; }
}

/// <summary>A point in frame pixel coordinates.</summary>
public record BbScannerPointDto
{
    public required float X { get; init; }

    public required float Y { get; init; }
}

/// <summary>An axis aligned rectangle in frame pixel coordinates.</summary>
public record BbScannerRectDto
{
    public required float X { get; init; }

    public required float Y { get; init; }

    public required float Width { get; init; }

    public required float Height { get; init; }
}

/// <summary>Where the time of one decoded frame went, read off BbBarcodeDecoder.LastFrame.</summary>
public record BbScannerTimingsDto
{
    public required double TotalMilliseconds { get; init; }

    public required double GrayscaleMilliseconds { get; init; }

    public required double BinarizeMilliseconds { get; init; }

    public required double MatrixMilliseconds { get; init; }

    public required double LinearMilliseconds { get; init; }

    /// <summary>How many scan lines the linear readers looked at.</summary>
    public required int RowsScanned { get; init; }
}

/// <summary>
/// One run of BbBarcodeDecoder over the rendered frame, and the BbBarcodeResult it returned - every member an
/// application reads, with the values the library really produced.
/// </summary>
public record BbScannerDecodeAttemptDto
{
    /// <summary>"live" (a new BbScannerOptions, the camera defaults) or "still" (BbScannerOptions.ForStillImages()).</summary>
    public required string Options { get; init; }

    public required bool Decoded { get; init; }

    /// <summary>BbBarcodeResult.Text.</summary>
    public string? Text { get; init; }

    /// <summary>BbBarcodeResult.Format.</summary>
    public string? Format { get; init; }

    /// <summary>True when Text is the payload that was encoded.</summary>
    public bool? Matches { get; init; }

    /// <summary>BbBarcodeResult.RawBytes as hex, cut after 64 bytes.</summary>
    public string? RawBytes { get; init; }

    public int? RawByteCount { get; init; }

    /// <summary>BbBarcodeResult.Corners, in frame pixel coordinates.</summary>
    public BbScannerPointDto[]? Corners { get; init; }

    /// <summary>BbBarcodeResult.BoundingBox.</summary>
    public BbScannerRectDto? BoundingBox { get; init; }

    /// <summary>The members of BbBarcodeResult.Metadata that the symbology filled in, by name.</summary>
    public Dictionary<string, string>? Metadata { get; init; }

    public required BbScannerTimingsDto Timings { get; init; }
}

/// <summary>
/// What the decode lab did: rendered a symbol with the test kit's encoders, put it through the camera-like
/// transforms asked for, and ran the library's own decoder over it with the live and the still image defaults.
/// </summary>
public record BbScannerDecodeReportDto
{
    /// <summary>The BbBarcodeFormat member that was encoded.</summary>
    public required string Format { get; init; }

    /// <summary>The payload that was encoded, after the lab completed it (an EAN or UPC check digit, for one).</summary>
    public required string Text { get; init; }

    /// <summary>False when the payload cannot be encoded in this symbology; Error then says why.</summary>
    public required bool Encoded { get; init; }

    public string? Error { get; init; }

    /// <summary>The frame the decoder saw, in pixels.</summary>
    public int? FrameWidth { get; init; }

    public int? FrameHeight { get; init; }

    /// <summary>Pixels per module of the rendered symbol.</summary>
    public int? ModuleSize { get; init; }

    /// <summary>The rotation applied to the rendered symbol, in degrees.</summary>
    public double? Rotation { get; init; }

    /// <summary>The blur applied, as a Gaussian sigma in pixels.</summary>
    public double? Blur { get; init; }

    public BbScannerDecodeAttemptDto[]? Attempts { get; init; }

    /// <summary>What the outcome means in practice, and what to change when it is not the one wanted.</summary>
    public required string[] Notes { get; init; }
}
