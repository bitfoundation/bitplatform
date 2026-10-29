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
        // Frames refreshed into reference slots but not shown, kept in case a show_existing_frame displays one.
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
                            for (var i = 0; i < slots.Length; i++)
                                if (ReferenceEquals(slots[i], existing)) slots[i] = null;
                            shown?.Dispose();
                            shown = existing;
                            hdr = null;
                            break;
                        }
                        if (!hdr.FrameIsIntra)
                        {
                            if (shown is null) throw new ImageDecodeException("The AV1 image uses inter prediction, which still image decoding does not support.");
                            shown.SkippedFrames = true;
                            return Take(ref shown);
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
            return shown is not null ? Take(ref shown) : throw new ImageDecodeException("The AV1 data holds no complete frame.");
        }
        finally
        {
            frame?.Dispose();
            shown?.Dispose();
            var disposed = new HashSet<Av1Image>(ReferenceEqualityComparer.Instance);
            foreach (var slot in slots)
                if (slot is not null && disposed.Add(slot)) slot.Dispose();
        }
    }

    private static Av1Image Take(ref Av1Image? image)
    {
        var taken = image!;
        image = null;
        return taken;
    }

    private static void Finish(ref Av1Frame? frame, ref Av1FrameHeader? hdr, ref Av1Image? shown, Av1Image?[] slots, CancellationToken cancellationToken)
    {
        var image = FinishFrame(frame!, cancellationToken);
        frame = null;
        if (hdr!.ShowFrame)
        {
            shown?.Dispose();
            shown = image;
        }
        else
        {
            StoreInSlots(slots, image, hdr.RefreshFrameFlags);
        }
        hdr = null;
    }

    private static void StoreInSlots(Av1Image?[] slots, Av1Image image, int refreshFrameFlags)
    {
        var stored = false;
        for (var i = 0; i < 8; i++)
        {
            if ((refreshFrameFlags & (1 << i)) == 0) continue;
            var old = slots[i];
            slots[i] = image;
            stored = true;
            if (old is not null && Array.IndexOf(slots, old) < 0) old.Dispose();
        }
        if (!stored) image.Dispose();
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
        try
        {
            var hdr = frame.Hdr;
            if (hdr.LoopFilterLevel[0] != 0 || hdr.LoopFilterLevel[1] != 0) Av1LoopFilter.Apply(frame, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var cdef = Av1Cdef.Apply(frame, cancellationToken); // null when CDEF changes nothing
            cancellationToken.ThrowIfCancellationRequested();
            Av1Plane[] planes;
            if (hdr.UseSuperres)
            {
                var upscaledCurr = Av1Restoration.Upscale(frame, frame.Planes);
                Av1Plane[]? upscaledCdef = null;
                if (cdef is not null)
                {
                    upscaledCdef = Av1Restoration.Upscale(frame, cdef);
                    foreach (var plane in cdef) plane.Dispose();
                }
                planes = Av1Restoration.Apply(frame, upscaledCurr, upscaledCdef ?? upscaledCurr, cancellationToken);
                if (!ReferenceEquals(planes, upscaledCurr)) foreach (var plane in upscaledCurr) plane.Dispose();
                if (upscaledCdef is not null && !ReferenceEquals(planes, upscaledCdef)) foreach (var plane in upscaledCdef) plane.Dispose();
            }
            else
            {
                planes = Av1Restoration.Apply(frame, frame.Planes, cdef ?? frame.Planes, cancellationToken);
                if (cdef is not null && !ReferenceEquals(planes, cdef)) foreach (var plane in cdef) plane.Dispose();
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
            };
            if (keepFramePlanes)
            {
                // The image now owns the frame's planes.
                for (var i = 0; i < frame.Planes.Length; i++) frame.Planes[i] = new Av1Plane(1, 1);
            }
            if (hdr.FilmGrain.ApplyGrain) Av1FilmGrainSynthesis.Apply(image, hdr.FilmGrain);
            return image;
        }
        finally
        {
            frame.Dispose();
        }
    }
}
