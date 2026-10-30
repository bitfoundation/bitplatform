namespace Bit.Bmage.Codecs.Avif;

/// <summary>A decoded AV1 frame: its planes cropped to the frame size in use, and the colour description to read them by.</summary>
internal sealed class Av1Image : IDisposable
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int BitDepth { get; init; }
    public required int SubX { get; init; }
    public required int SubY { get; init; }
    public required bool MonoChrome { get; init; }
    public required Av1SequenceHeader Seq { get; init; }
    public required Av1Plane[] Planes { get; init; }

    /// <summary>The frame's current_frame_id, by which a stream that numbers its frames names it to show it again.</summary>
    public int FrameId { get; init; }

    /// <summary>True when frames after this one were left undecoded because they use inter prediction.</summary>
    public bool SkippedFrames { get; set; }

    public void Dispose()
    {
        foreach (var plane in Planes) plane.Dispose();
    }
}

/// <summary>
/// Decodes the intra frames of an AV1 bitstream (a sequence of OBUs, spec section 5.3), which is what an AVIF image item
/// holds, returning the last one shown. Decoding stops at the first inter frame, such as the enhancement layer of a
/// layered (progressive) image, and the result then says so.
/// </summary>
internal static class Av1Decoder
{

    private const int ObuSequenceHeader = 1, ObuTemporalDelimiter = 2, ObuFrameHeader = 3, ObuTileGroup = 4, ObuFrame = 6, ObuRedundantFrameHeader = 7;

    public static Av1Image Decode(byte[] data, int offset, int length, ImageLimits limits, CancellationToken cancellationToken)
    {
        Av1SequenceHeader? seq = null;
        Av1FrameHeader? hdr = null;
        Av1Frame? frame = null;
        Av1TileDecoder? tiles = null;
        Av1Image? shown = null;
        var nextTile = 0;
        // The frames refresh_frame_flags put in the reference slots, shown or not, kept in case a show_existing_frame
        // displays one. An image may sit in several slots and be the shown one too, so each is disposed only once none
        // of them holds it.
        var slots = new Av1Image?[8];
        try
        {
            var pos = offset;
            var end = offset + length;
            while (pos < end)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var header = data[pos];
                if ((header & 0x80) != 0) throw new ImageDecodeException("Invalid AV1 OBU header.");
                var obuType = (header >> 3) & 15;
                var hasExtension = (header & 4) != 0;
                var hasSize = (header & 2) != 0;
                var temporalId = 0;
                var spatialId = 0;
                var p = pos + 1;
                if (hasExtension)
                {
                    if (p >= end) throw new ImageDecodeException("Truncated AV1 OBU.");
                    temporalId = data[p] >> 5;
                    spatialId = (data[p] >> 3) & 3;
                    p++;
                }
                long obuSize;
                if (hasSize)
                {
                    var n = Av1BitReader.ReadLeb128(data.AsSpan(0, end), p, out var size);
                    if (n == 0) throw new ImageDecodeException("Invalid AV1 OBU size.");
                    p += n;
                    obuSize = (long)Math.Min(size, int.MaxValue);
                }
                else
                {
                    obuSize = end - p;
                }
                if (obuSize > end - p) throw new ImageDecodeException("Truncated AV1 OBU.");
                var payload = p;
                var payloadSize = (int)obuSize;
                pos = p + payloadSize;

                if (obuType != ObuSequenceHeader && obuType != ObuTemporalDelimiter && hasExtension && seq is { OperatingPointIdc: not 0 } s)
                {
                    var inTemporal = (s.OperatingPointIdc >> temporalId) & 1;
                    var inSpatial = (s.OperatingPointIdc >> (spatialId + 8)) & 1;
                    if (inTemporal == 0 || inSpatial == 0) continue;
                }

                switch (obuType)
                {
                    case ObuSequenceHeader:
                        seq = Av1SequenceHeader.Parse(data.AsSpan(payload, payloadSize));
                        break;
                    case ObuTemporalDelimiter:
                        hdr = null;
                        break;
                    case ObuFrameHeader:
                    case ObuRedundantFrameHeader:
                    case ObuFrame:
                    {
                        // A frame header OBU repeated while its tile groups are still arriving is a copy of the one in use.
                        if (obuType != ObuFrame && hdr is not null && frame is not null) break;
                        if (seq is null) throw new ImageDecodeException("AV1 frame header before any sequence header.");
                        var reader = new Av1BitReader(data.AsSpan(payload, payloadSize));
                        hdr = Av1FrameHeader.Parse(ref reader, seq, temporalId, spatialId);
                        if (hdr.ShowExistingFrame)
                        {
                            var existing = slots[hdr.FrameToShowMapIdx] ?? throw new ImageDecodeException("The AV1 stream shows a frame it never decoded.");
                            if (seq.FrameIdNumbersPresent && existing.FrameId != hdr.DisplayFrameId)
                                throw new ImageDecodeException("The AV1 stream shows a frame other than the one it names.");
                            var previous = shown;
                            shown = existing;
                            Release(previous, shown, slots);
                            hdr = null;
                            break;
                        }
                        if (!hdr.FrameIsIntra)
                        {
                            if (shown is null) throw new ImageDecodeException("The AV1 image uses inter prediction, which still image decoding does not support.");
                            shown.SkippedFrames = true;
                            return Take(ref shown, slots);
                        }
                        limits.Validate(hdr.UpscaledWidth, hdr.FrameHeight);
                        frame?.Dispose();
                        frame = new Av1Frame(seq, hdr);
                        tiles = new Av1TileDecoder(frame);
                        nextTile = 0;
                        if (obuType == ObuFrame)
                        {
                            reader.ByteAlign();
                            var headerBytes = (int)(reader.BitPosition >> 3);
                            if (DecodeTileGroup(data, payload + headerBytes, payloadSize - headerBytes, hdr, tiles, ref nextTile, cancellationToken))
                                Finish(ref frame, ref hdr, ref shown, slots, cancellationToken);
                        }
                        break;
                    }
                    case ObuTileGroup:
                        if (hdr is null || frame is null || tiles is null) throw new ImageDecodeException("AV1 tile group without a frame header.");
                        if (DecodeTileGroup(data, payload, payloadSize, hdr, tiles, ref nextTile, cancellationToken))
                            Finish(ref frame, ref hdr, ref shown, slots, cancellationToken);
                        break;
                }
            }
            return shown is not null ? Take(ref shown, slots) : throw new ImageDecodeException("The AV1 data holds no complete frame.");
        }
        finally
        {
            frame?.Dispose();
            var disposed = new HashSet<Av1Image>(ReferenceEqualityComparer.Instance);
            if (shown is not null && disposed.Add(shown)) shown.Dispose();
            foreach (var slot in slots)
                if (slot is not null && disposed.Add(slot)) slot.Dispose();
        }
    }

    /// <summary>Hands the image to the caller, taking it out of the slots, which are released afterwards.</summary>
    private static Av1Image Take(ref Av1Image? image, Av1Image?[] slots)
    {
        var taken = image!;
        image = null;
        for (var i = 0; i < slots.Length; i++)
            if (ReferenceEquals(slots[i], taken)) slots[i] = null;
        return taken;
    }

    /// <summary>Disposes an image that is neither shown nor in any slot any more.</summary>
    private static void Release(Av1Image? image, Av1Image? shown, Av1Image?[] slots)
    {
        if (image is not null && !ReferenceEquals(image, shown) && Array.IndexOf(slots, image) < 0) image.Dispose();
    }

    private static void Finish(ref Av1Frame? frame, ref Av1FrameHeader? hdr, ref Av1Image? shown, Av1Image?[] slots, CancellationToken cancellationToken)
    {
        var image = FinishFrame(frame!, cancellationToken);
        frame = null;
        var previous = shown;
        if (hdr!.ShowFrame) shown = image;
        // Shown or not, the frame goes to the slots refresh_frame_flags names, all of them for a shown key frame.
        StoreInSlots(slots, image, hdr.RefreshFrameFlags, shown);
        Release(previous, shown, slots);
        hdr = null;
    }

    private static void StoreInSlots(Av1Image?[] slots, Av1Image image, int refreshFrameFlags, Av1Image? shown)
    {
        for (var i = 0; i < 8; i++)
        {
            if ((refreshFrameFlags & (1 << i)) == 0) continue;
            var old = slots[i];
            slots[i] = image;
            Release(old, shown, slots);
        }
        Release(image, shown, slots);
    }

    /// <summary>The first sequence header among <paramref name="obus"/>, or null when there is none.</summary>
    public static Av1SequenceHeader? FindSequenceHeader(ReadOnlySpan<byte> obus)
    {
        var pos = 0;
        while (pos < obus.Length)
        {
            var header = obus[pos];
            if ((header & 0x80) != 0) return null;
            var p = pos + 1 + ((header & 4) != 0 ? 1 : 0);
            long size;
            if ((header & 2) != 0)
            {
                var n = Av1BitReader.ReadLeb128(obus, p, out var value);
                if (n == 0) return null;
                p += n;
                size = (long)Math.Min(value, int.MaxValue);
            }
            else
            {
                size = obus.Length - p;
            }
            if (p > obus.Length || size > obus.Length - p) return null;
            if (((header >> 3) & 15) == ObuSequenceHeader) return Av1SequenceHeader.Parse(obus.Slice(p, (int)size));
            pos = p + (int)size;
        }
        return null;
    }

    /// <summary>Decodes the tiles of one tile group, returning true when it was the last of the frame.</summary>
    private static bool DecodeTileGroup(byte[] data, int start, int size, Av1FrameHeader hdr, Av1TileDecoder tiles, ref int nextTile, CancellationToken cancellationToken)
    {
        if (size < 0) throw new ImageDecodeException("Truncated AV1 tile group.");
        var numTiles = hdr.TileCols * hdr.TileRows;
        var reader = new Av1BitReader(data.AsSpan(start, size));
        int tgStart, tgEnd;
        var startAndEndPresent = numTiles > 1 && reader.ReadFlag();
        if (numTiles == 1 || !startAndEndPresent)
        {
            tgStart = 0;
            tgEnd = numTiles - 1;
        }
        else
        {
            var tileBits = hdr.TileColsLog2 + hdr.TileRowsLog2;
            tgStart = reader.Read(tileBits);
            tgEnd = reader.Read(tileBits);
        }
        reader.ByteAlign();
        if (tgStart != nextTile || tgEnd < tgStart || tgEnd >= numTiles) throw new ImageDecodeException("Invalid AV1 tile group.");
        var pos = start + (int)(reader.BitPosition >> 3);
        var remaining = size - (int)(reader.BitPosition >> 3);
        for (var tileNum = tgStart; tileNum <= tgEnd; tileNum++)
        {
            int tileSize;
            if (tileNum == tgEnd)
            {
                tileSize = remaining;
            }
            else
            {
                if (remaining < hdr.TileSizeBytes) throw new ImageDecodeException("Truncated AV1 tile.");
                var v = 0L;
                for (var i = 0; i < hdr.TileSizeBytes; i++) v |= (long)data[pos + i] << (8 * i);
                tileSize = (int)Math.Min(v + 1, int.MaxValue);
                pos += hdr.TileSizeBytes;
                remaining -= hdr.TileSizeBytes;
            }
            if (tileSize <= 0 || tileSize > remaining) throw new ImageDecodeException("Truncated AV1 tile.");
            tiles.DecodeTile(data, pos, tileSize, tileNum / hdr.TileCols, tileNum % hdr.TileCols, cancellationToken);
            pos += tileSize;
            remaining -= tileSize;
        }
        nextTile = tgEnd + 1;
        return tgEnd == numTiles - 1;
    }

    /// <summary>decode_frame_wrapup and the output process: loop filter, CDEF, superres, loop restoration and film grain.</summary>
    private static Av1Image FinishFrame(Av1Frame frame, CancellationToken cancellationToken)
    {
        // The planes made on the way, each released at the end unless it is what the image is made of, whether or not
        // decoding got that far.
        Av1Plane[]? cdef = null, upscaledCurr = null, upscaledCdef = null, planes = null;
        try
        {
            var hdr = frame.Hdr;
            if (hdr.LoopFilterLevel[0] != 0 || hdr.LoopFilterLevel[1] != 0) Av1LoopFilter.Apply(frame, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            cdef = Av1Cdef.Apply(frame, cancellationToken); // null when CDEF changes nothing
            cancellationToken.ThrowIfCancellationRequested();
            if (hdr.UseSuperres)
            {
                upscaledCurr = Av1Restoration.Upscale(frame, frame.Planes);
                if (cdef is not null)
                {
                    upscaledCdef = Av1Restoration.Upscale(frame, cdef);
                    foreach (var plane in cdef) plane.Dispose();
                    cdef = null;
                }
                planes = Av1Restoration.Apply(frame, upscaledCurr, upscaledCdef ?? upscaledCurr, cancellationToken);
            }
            else
            {
                planes = Av1Restoration.Apply(frame, frame.Planes, cdef ?? frame.Planes, cancellationToken);
            }
            var keepFramePlanes = ReferenceEquals(planes, frame.Planes);
            var image = new Av1Image
            {
                Width = hdr.UpscaledWidth,
                Height = hdr.FrameHeight,
                BitDepth = frame.BitDepth,
                SubX = frame.SubX,
                SubY = frame.SubY,
                MonoChrome = frame.Seq.MonoChrome,
                Seq = frame.Seq,
                Planes = keepFramePlanes ? (Av1Plane[])planes.Clone() : planes,
                FrameId = hdr.CurrentFrameId,
            };
            if (keepFramePlanes)
            {
                // The image now owns the frame's planes.
                for (var i = 0; i < frame.Planes.Length; i++) frame.Planes[i] = new Av1Plane(1, 1);
            }
            try
            {
                if (hdr.FilmGrain.ApplyGrain) Av1FilmGrainSynthesis.Apply(image, hdr.FilmGrain, cancellationToken);
            }
            catch
            {
                image.Dispose();
                throw;
            }
            return image;
        }
        finally
        {
            DisposeUnlessKept(cdef, planes);
            DisposeUnlessKept(upscaledCurr, planes);
            DisposeUnlessKept(upscaledCdef, planes);
            frame.Dispose();
        }
    }

    private static void DisposeUnlessKept(Av1Plane[]? planes, Av1Plane[]? kept)
    {
        if (planes is not null && !ReferenceEquals(planes, kept)) foreach (var plane in planes) plane.Dispose();
    }
}
