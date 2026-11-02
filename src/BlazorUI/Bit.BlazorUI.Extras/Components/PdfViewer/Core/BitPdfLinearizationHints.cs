namespace Bit.BlazorUI;

// ISO 32000-1 Annex F: hints are advisory acquisition ranges, never xref entries
// or proof of page-tree order. Every bit count, allocation and file range is bounded.
internal sealed class BitPdfLinearizationHints
{
    internal sealed record Header(int PageCount, int FirstPage, int FirstObject, int FirstEnd,
        (int Offset, int Length)[] Streams);
    private sealed record Page(int Object, int Offset, int Length, int[] Shared);
    private readonly Page[] _pages;
    private readonly (int Offset, int Length)[] _shared;
    private BitPdfLinearizationHints(Page[] pages, (int Offset, int Length)[] shared)
    { _pages = pages; _shared = shared; }

    internal List<(int Offset, int Length)> Ranges(int page, int maxBytes)
    {
        var ranges = new List<(int Offset, int Length)>();
        if ((uint)page >= (uint)_pages.Length) return ranges;
        var hint = _pages[page];
        long total = hint.Length;
        if (total > maxBytes) return ranges;
        ranges.Add((hint.Offset, hint.Length));
        var seen = new HashSet<int>();
        foreach (int id in hint.Shared)
        {
            if (!seen.Add(id)) continue;
            total += _shared[id].Length;
            if (total > maxBytes) return [];
            ranges.Add(_shared[id]);
        }
        return ranges;
    }

    internal static BitPdfLinearizationHints Read(byte[] data, int sharedOffset, Header header, int fileLength,
        Func<int, int, bool> matchesObject)
    {
        if (header.PageCount is < 1 or > 100_000 || sharedOffset < 36 || sharedOffset > data.Length - 24)
            throw Invalid();
        var bits = new Bits(data, 0, sharedOffset);
        int leastObjects = bits.Positive();
        int firstHintOffset = bits.Number();
        int firstOffset = FileOffset(firstHintOffset);
        int objectBits = bits.Width();
        int leastLength = bits.Positive();
        int lengthBits = bits.Width();
        int leastContentOffset = bits.Number(), contentOffsetBits = bits.Width();
        int leastContentLength = bits.Number(), contentLengthBits = bits.Width();
        int sharedCountBits = bits.Width(), sharedIdBits = bits.Width(), numeratorBits = bits.Width();
        bits.Read(16); // Shared-reference position denominator (positions are not needed for prefetch).
        int count = header.PageCount;
        var objects = new int[count];
        var numbers = new int[count];
        long number = 1;
        for (int i = 0; i < count; i++)
        {
            objects[i] = Sum(leastObjects, bits.Read(objectBits));
            if (i == header.FirstPage) numbers[i] = header.FirstObject;
            else { numbers[i] = Sum(number, 0); number += objects[i]; }
        }
        bits.Align();
        var lengths = new int[count];
        for (int i = 0; i < count; i++) lengths[i] = Sum(leastLength, bits.Read(lengthBits));
        bits.Align();
        var references = new int[count][];
        long referenceCount = 0;
        for (int i = 0; i < count; i++)
        {
            int n = bits.Number(sharedCountBits);
            referenceCount += n;
            if (referenceCount > 1_000_000) throw Invalid();
            references[i] = new int[n];
        }
        bits.Align();
        for (int i = 0; i < count; i++)
            for (int j = 0; j < references[i].Length; j++) references[i][j] = bits.Number(sharedIdBits);
        bits.Align();
        bits.Skip(referenceCount * numeratorBits); bits.Align();
        // Content ranges are not used, but still validate their representation and page bounds.
        var contentOffsets = new int[count];
        for (int i = 0; i < count; i++) contentOffsets[i] = Sum(leastContentOffset, bits.Read(contentOffsetBits));
        bits.Align();
        for (int i = 0; i < count; i++)
            if ((long)contentOffsets[i] + Sum(leastContentLength, bits.Read(contentLengthBits)) > lengths[i]) throw Invalid();
        bits.Align();

        var sharedBits = new Bits(data, sharedOffset, data.Length);
        int firstSharedObject = sharedBits.Number();
        int firstSharedHintOffset = sharedBits.Number();
        int firstPageShared = sharedBits.Number(), totalShared = sharedBits.Number();
        int groupObjectBits = sharedBits.Width();
        int leastGroupLength = sharedBits.Number(), groupLengthBits = sharedBits.Width();
        if (totalShared > 1_000_000 || firstPageShared > totalShared) throw Invalid();
        if (totalShared > firstPageShared) FileOffset(firstSharedHintOffset);
        var shared = new (int Offset, int Length)[totalShared];
        long sharedPosition = firstHintOffset;
        for (int i = 0; i < totalShared; i++)
        {
            if (i == firstPageShared) sharedPosition = firstSharedHintOffset;
            int length = Sum(leastGroupLength, sharedBits.Read(groupLengthBits));
            shared[i] = HintRange(sharedPosition, length);
            sharedPosition += length;
        }
        sharedBits.Align();
        long signatures = 0;
        for (int i = 0; i < totalShared; i++) signatures += sharedBits.Read(1);
        sharedBits.Align(); sharedBits.Skip(signatures * 128); sharedBits.Align();
        long sharedNumber = header.FirstObject;
        for (int i = 0; i < totalShared; i++)
        {
            if (i == firstPageShared) sharedNumber = firstSharedObject;
            int groupCount = Sum(1, sharedBits.Read(groupObjectBits));
            if (sharedNumber <= 0 || !matchesObject(Sum(sharedNumber, 0), shared[i].Offset)) throw Invalid();
            sharedNumber += groupCount;
        }
        sharedBits.Align();
        var pages = new Page[count];
        long position = header.FirstEnd;
        foreach (var stream in header.Streams)
            if ((long)stream.Offset + stream.Length <= header.FirstEnd) position -= stream.Length;
        for (int i = 0; i < count; i++)
        {
            long hintOffset = i == header.FirstPage ? firstHintOffset : position;
            var range = HintRange(hintOffset, lengths[i]);
            int offset = range.Offset;
            if (!matchesObject(numbers[i], offset)) throw Invalid();
            foreach (int id in references[i]) if ((uint)id >= (uint)shared.Length) throw Invalid();
            pages[i] = new(numbers[i], range.Offset, range.Length, references[i]);
            if (i != header.FirstPage) position += lengths[i];
        }
        return new(pages, shared);

        int FileOffset(long offset, bool allowEnd = false)
        {
            long actual = offset;
            // Hint offsets describe the file with both hint stream objects removed.
            foreach (var stream in header.Streams.OrderBy(s => s.Offset))
                if (actual >= stream.Offset) actual += stream.Length;
            if (actual <= 0 || actual > fileLength || (!allowEnd && actual == fileLength)) throw Invalid();
            return (int)actual;
        }
        (int Offset, int Length) HintRange(long offset, int length)
        {
            if (length <= 0) throw Invalid();
            int start = FileOffset(offset);
            int end = FileOffset(offset + length, allowEnd: true);
            return (start, end - start);
        }
    }

    private static int Sum(long value, uint delta)
    {
        long result = value + delta;
        if (result < 0 || result > int.MaxValue) throw Invalid();
        return (int)result;
    }
    private static BitPdfFormatException Invalid() => new("The optional PDF linearization hints are unusable.");
    private sealed class Bits(byte[] data, int start, int end)
    {
        private long _position = (long)start * 8;
        internal uint Read(int width)
        {
            if (width is < 0 or > 32 || _position + width > (long)end * 8) throw Invalid();
            uint value = 0;
            for (int i = 0; i < width; i++, _position++)
                value = (value << 1) | (uint)((data[(int)(_position / 8)] >> (7 - (int)(_position % 8))) & 1);
            return value;
        }
        internal int Width() { int value = (int)Read(16); if (value > 32) throw Invalid(); return value; }
        internal int Number(int width = 32) => Sum(0, Read(width));
        internal int Positive() { int value = Number(); if (value == 0) throw Invalid(); return value; }
        internal void Skip(long count)
        { if (count < 0 || count > (long)end * 8 - _position) throw Invalid(); _position += count; }
        internal void Align() => Skip((8 - _position % 8) % 8);
    }
}
