using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfByteSourceTests
{
    [TestMethod]
    public void ByteSourcesShouldRejectNullBuffersAndReaders()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new BitPdfMemoryByteSource(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BitPdfByteSourceReader(null!));
    }

    [TestMethod]
    public void MemoryReaderShouldPreserveBorrowedBytesAndBoundsAtEof()
    {
        byte[] bytes = [10, 20, 30];
        var reader = new BitPdfByteSourceReader(new BitPdfMemoryByteSource(bytes));

        Assert.AreEqual(20, reader.ReadByte(1));
        bytes[1] = 42;
        Assert.AreEqual(42, reader.ReadByte(1));
        Assert.AreEqual(-1, reader.ReadByte(3));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => reader.ReadByte(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => reader.ReadByte(4));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => reader.ReadByte(int.MaxValue));
        Assert.AreEqual(-1, new BitPdfByteSourceReader(new BitPdfMemoryByteSource([])).ReadByte(0));
    }

    [TestMethod]
    public void MemorySourceShouldReadBytesAndEofWithoutACursor()
    {
        var source = new BitPdfMemoryByteSource([10, 20, 30]);

        Assert.AreEqual(3, source.Length);
        Assert.AreEqual(30, source.ReadByte(2));
        Assert.AreEqual(10, source.ReadByte(0));
        Assert.AreEqual(-1, source.ReadByte(3));
        Assert.AreEqual(-1, new BitPdfMemoryByteSource([]).ReadByte(0));
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(4)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void MemorySourceShouldRejectOutOfBoundsOffsets(int offset)
    {
        var source = new BitPdfMemoryByteSource([10, 20, 30]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.ReadByte(offset));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.Read(offset, new byte[1]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.GetMemory(offset, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.CreateStream(offset));
    }

    [TestMethod]
    public void MemorySourceShouldClampCopiedReadsAtEofAndLeaveTheRestUntouched()
    {
        byte[] original = [10, 20, 30];
        var source = new BitPdfMemoryByteSource(original);
        byte[] destination = [99, 99, 99, 99];

        Assert.AreEqual(2, source.Read(1, destination));
        CollectionAssert.AreEqual(new byte[] { 20, 30, 99, 99 }, destination);
        destination[0] = 77;
        Assert.AreEqual(20, source.ReadByte(1));
        Assert.AreEqual(0, source.Read(3, destination));
        Assert.AreEqual(0, source.Read(0, Span<byte>.Empty));
    }

    [TestMethod]
    public void MemorySourceShouldBorrowExactViewsOfTheExistingBuffer()
    {
        byte[] original = [10, 20, 30];
        var source = new BitPdfMemoryByteSource(original);
        var view = source.GetMemory(1, 2);

        CollectionAssert.AreEqual(new byte[] { 20, 30 }, view.ToArray());
        // Pin the existing borrowed-buffer contract; this is not permission to
        // mutate a document while parsing it.
        original[1] = 42;
        Assert.AreEqual(42, view.Span[0]);
        Assert.IsTrue(source.GetMemory(3, 0).IsEmpty);
    }

    [TestMethod]
    [DataRow(0, -1)]
    [DataRow(2, 2)]
    [DataRow(3, 1)]
    [DataRow(1, int.MaxValue)]
    public void MemorySourceShouldRejectInvalidOrOverflowingWindows(int start, int length)
    {
        var source = new BitPdfMemoryByteSource([10, 20, 30]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.GetMemory(start, length));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.CreateStream(start, length));
    }

    [TestMethod]
    public void MemorySourceShouldCreateIndependentCursorsWithoutChangingPublicStreamBehavior()
    {
        byte[] original = [10, 20, 30, 40];
        var source = new BitPdfMemoryByteSource(original);
        var dict = new BitPdfDict();
        var first = source.CreateStream(1, 2, dict);
        var second = source.CreateStream(1);

        Assert.IsInstanceOfType<BitPdfStream>(first);
        Assert.AreSame(original, ((BitPdfStream)first).Buffer);
        Assert.AreSame(dict, first.Dict);
        Assert.AreEqual(1, first.Start);
        Assert.AreEqual(3, first.End);
        Assert.AreEqual(20, first.PeekByte());
        Assert.AreEqual(1, first.Pos);
        Assert.AreEqual(20, first.GetByte());
        Assert.AreEqual(20, second.GetByte());
        CollectionAssert.AreEqual(new byte[] { 30 }, first.GetBytes(10));
        Assert.AreEqual(-1, first.GetByte());
        first.Reset();
        Assert.AreEqual(20, first.GetByte());

        // Absolute range reads historically clamp to the buffer, not the window.
        CollectionAssert.AreEqual(original, first.GetByteRange(-1, 99));
        Assert.AreEqual(2, first.Pos);
        var sub = first.MakeSubStream(2, 1, dict);
        Assert.AreEqual(30, sub.GetByte());
        Assert.AreEqual(-1, sub.GetByte());
        Assert.AreSame(dict, sub.Dict);
        Assert.AreSame(original, ((BitPdfStream)sub).Buffer);
        Assert.AreEqual(-1, source.CreateStream(source.Length).GetByte());
    }

    [TestMethod]
    public void BlockReaderShouldScanAndSeekWithoutPerByteSourceCalls()
    {
        var source = new CountingSource(150000);
        var reader = new BitPdfByteSourceReader(source);

        for (int i = 0; i < source.Length; i++)
        {
            Assert.AreEqual(i & 255, reader.ReadByte(i));
        }
        Assert.AreEqual(-1, reader.ReadByte(source.Length));
        Assert.IsTrue(source.Views.Count < 10, "Sequential scanning must use bounded block access.");
        foreach (var view in source.Views)
        {
            Assert.IsTrue(view.Length > 0 && view.Length <= 64 * 1024);
            Assert.IsTrue(view.Start >= 0 && view.Length <= source.Length - view.Start);
        }
        Assert.AreEqual(0, reader.ReadByte(0));
        Assert.AreEqual(70000 & 255, reader.ReadByte(70000));
        Assert.AreEqual(10, reader.ReadByte(10));
    }

    [TestMethod]
    public void BlockReaderShouldKeepItsPreviousViewWhenAReadFails()
    {
        var source = new CountingSource(150000);
        var reader = new BitPdfByteSourceReader(source);
        Assert.AreEqual(10, reader.ReadByte(10));

        source.FailNextRead = true;
        Assert.ThrowsExactly<InvalidOperationException>(() => reader.ReadByte(100000));
        int requestsAfterFailure = source.Views.Count;
        Assert.AreEqual(10, reader.ReadByte(10));
        Assert.AreEqual(requestsAfterFailure, source.Views.Count);
        Assert.AreEqual(100000 & 255, reader.ReadByte(100000));
    }

    [TestMethod]
    public void BlockReaderShouldBoundsCheckNearTheLargestSupportedOffset()
    {
        var source = new CountingSource(int.MaxValue);
        var reader = new BitPdfByteSourceReader(source);

        Assert.AreEqual((int.MaxValue - 1) & 255, reader.ReadByte(int.MaxValue - 1));
        Assert.AreEqual(-1, reader.ReadByte(int.MaxValue));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => reader.ReadByte(int.MinValue));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => reader.ReadByte(-1));
        Assert.AreEqual(1, source.Views.Count);

        var empty = new BitPdfByteSourceReader(new CountingSource(0));
        Assert.AreEqual(-1, empty.ReadByte(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => empty.ReadByte(1));
    }

    [TestMethod]
    public void ReaderShouldFindKeywordsAcrossBlocksAndAtEofWithoutSkippingMatches()
    {
        byte[] bytes = new byte[131080];
        Array.Fill(bytes, (byte)' ');
        byte[] keyword = "endstream"u8.ToArray();
        keyword.CopyTo(bytes.AsSpan(65533));
        int last = bytes.Length - keyword.Length;
        keyword.CopyTo(bytes.AsSpan(last));
        var source = new CountingSource(bytes.Length) { Content = bytes };
        var reader = new BitPdfByteSourceReader(source);
        var memoryReader = new BitPdfByteSourceReader(new BitPdfMemoryByteSource(bytes));

        foreach (var current in new[] { reader, memoryReader })
        {
            Assert.AreEqual(65533, current.IndexOf(keyword, 0));
            Assert.AreEqual(65533, current.IndexOf(keyword, 65533));
            Assert.AreEqual(last, current.IndexOf(keyword, 65534));
            Assert.AreEqual(-1, current.IndexOf(keyword, last + 1));
            Assert.AreEqual(-1, current.IndexOf(keyword, bytes.Length));
            Assert.AreEqual(bytes.Length, current.IndexOf([], bytes.Length));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => current.IndexOf(keyword, -1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => current.IndexOf(keyword, bytes.Length + 1));
        }
        foreach (var view in source.Views)
        {
            Assert.IsTrue(view.Length <= 64 * 1024 + keyword.Length - 1);
            Assert.IsTrue(view.Start >= 0 && view.Length <= source.Length - view.Start);
        }
    }

    private sealed class CountingSource(int length) : IBitPdfByteSource
    {
        public int Length { get; } = length;
        public List<(int Start, int Length)> Views { get; } = [];
        public bool FailNextRead { get; set; }
        public byte[]? Content { get; init; }

        public ReadOnlyMemory<byte> GetMemory(int offset, int count)
        {
            Views.Add((offset, count));
            if (FailNextRead)
            {
                FailNextRead = false;
                throw new InvalidOperationException("Simulated source failure.");
            }
            Assert.IsTrue(offset >= 0 && offset <= Length && count >= 0 && count <= Length - offset);
            if (Content is not null)
            {
                return Content.AsMemory(offset, count);
            }
            var bytes = new byte[count];
            for (int i = 0; i < count; i++)
            {
                bytes[i] = (byte)((offset + i) & 255);
            }
            return bytes;
        }

        public int ReadByte(int offset) => throw new AssertFailedException("Unexpected per-byte source dispatch.");
        public int Read(int offset, Span<byte> destination) => throw new AssertFailedException("Unexpected copy.");
        public BitPdfBaseStream CreateStream(int start = 0, int? length = null, BitPdfDict? dict = null)
            => throw new AssertFailedException("Unexpected stream creation.");
    }
}
