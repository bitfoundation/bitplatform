using System.Text;

namespace Bit.BbScanner.Tests;

/// <summary>
/// Structured append: the metadata a QR Code, Data Matrix or Macro PDF417 symbol reports about its sequence, and
/// <see cref="BbStructuredAppendAssembler"/> putting a sequence back together - in any order, once, and never out of
/// symbols that do not belong together.
/// </summary>
[TestClass]
public class StructuredAppendTests
{
    private static readonly int[] FileA = [17, 300];
    private static readonly int[] FileB = [17, 301];

    private static int Parity(string message) => Encoding.UTF8.GetBytes(message).Aggregate(0, (parity, value) => parity ^ value);

    private static BbBarcodeResult Decode(BbBitMatrix matrix, BbBarcodeFormat format)
    {
        using (matrix)
        {
            using var image = SyntheticImage.FromMatrix(matrix, scale: 4);
            var options = BbScannerOptions.ForStillImages();
            options.Formats = format;
            using var decoder = new BbBarcodeDecoder(options);
            return decoder.Decode(image.View, suppressDuplicates: false) ?? throw new AssertFailedException($"The {format} symbol was not read.");
        }
    }

    private static BbBarcodeResult Qr(string part, int index, int count, int parity) =>
        Decode(QrEncoder.Encode(part, structuredAppend: (index, count, parity)), BbBarcodeFormat.QrCode);

    private static BbBarcodeResult[] QrSequence(string message, params string[] parts)
    {
        Assert.AreEqual(message, string.Concat(parts));
        var parity = Parity(message);
        return [.. parts.Select((part, index) => Qr(part, index, parts.Length, parity))];
    }

    private static BbBarcodeResult DataMatrix(string part, int position, int count, int fileId, int? eci = null) =>
        Decode(DataMatrixEncoder.Encode(part, structuredAppend: (position, count, fileId), eci: eci), BbBarcodeFormat.DataMatrix);

    private static BbBarcodeResult Pdf417(string part, int index, int[] fileId, int? count = null, bool last = false) =>
        Decode(Pdf417Encoder.Encode(part, columns: 4, macro: (index, fileId, count, last)), BbBarcodeFormat.Pdf417);

    [TestMethod]
    public void AQrSymbolReportsItsPlaceInTheSequence()
    {
        var parity = Parity("ABCDEF");
        var first = Qr("ABC", 0, 2, parity);
        var second = Qr("DEF", 1, 2, parity);

        Assert.AreEqual("ABC", first.Text);
        Assert.AreEqual(0, first.Metadata!.StructuredAppendIndex);
        Assert.AreEqual(2, first.Metadata.StructuredAppendCount);
        Assert.AreEqual(parity, first.Metadata.StructuredAppendParity);
        Assert.IsFalse(first.Metadata.StructuredAppendIsLast);
        Assert.IsNull(first.Metadata.StructuredAppendFileId);
        Assert.IsTrue(second.Metadata!.StructuredAppendIsLast);
    }

    [TestMethod]
    public void ADataMatrixSymbolCountsItsPlaceFromOne()
    {
        var first = DataMatrix("PART1", 1, 2, 0x1234);
        var second = DataMatrix("PART2", 2, 2, 0x1234);

        Assert.AreEqual(1, first.Metadata!.StructuredAppendIndex);
        Assert.IsFalse(first.Metadata.StructuredAppendIsLast);
        Assert.AreEqual(2, second.Metadata!.StructuredAppendIndex);
        Assert.IsTrue(second.Metadata.StructuredAppendIsLast);
        Assert.AreEqual(0x1234, second.Metadata.StructuredAppendParity);
    }

    [TestMethod]
    public void AMacroPdf417SegmentReportsItsFileIdAndWhetherItIsTheLast()
    {
        var first = Pdf417("FIRST", 0, FileA);
        var last = Pdf417("LAST", 1, FileA, last: true);

        Assert.AreEqual("FIRST", first.Text);
        Assert.AreEqual(0, first.Metadata!.StructuredAppendIndex);
        Assert.AreEqual("017300", first.Metadata.StructuredAppendFileId);
        Assert.IsFalse(first.Metadata.StructuredAppendIsLast);
        Assert.IsNull(first.Metadata.StructuredAppendCount);
        Assert.IsTrue(last.Metadata!.StructuredAppendIsLast);
    }

    [TestMethod]
    public void TheParserReadsTheMacroPdf417FileIdAndTerminator()
    {
        // Payload "A" in text, then 928, segment index 5, file id codewords 100 and 200, optional field 1 holding the
        // segment count 6, terminator.
        int[] codewords = [13, 900, (0 * 30) + 29, 928, 111, 105, 100, 200, 923, 1, 111, 106, 922];

        var result = BbPdf417BitStreamParser.Decode(codewords, 3, 4, 0, 0)!;

        Assert.AreEqual("100200", result.StructuredAppendFileId);
        Assert.IsTrue(result.StructuredAppendIsLast);
        Assert.IsNull(BbPdf417BitStreamParser.Decode([3, 900, 29], 3, 4, 0, 0)!.StructuredAppendIsLast, "No control block, no sequence.");
    }

    [TestMethod]
    public void AQrSequenceIsAssembledInWhateverOrderItIsScanned()
    {
        var symbols = QrSequence("Hello, structured world", "Hello, ", "structured ", "world");
        var assembler = new BbStructuredAppendAssembler();

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(symbols[2], out var message));
        Assert.IsNull(message);
        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(symbols[0], out _));

        var pending = assembler.Pending.Single();
        Assert.AreEqual(BbBarcodeFormat.QrCode, pending.Format);
        Assert.AreEqual(2, pending.Received);
        Assert.AreEqual(3, pending.Count);
        CollectionAssert.AreEqual(new[] { 1 }, pending.Missing.ToArray());

        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(symbols[1], out message));
        Assert.IsNotNull(message);
        Assert.AreEqual("Hello, structured world", message.Text);
        Assert.AreEqual(BbBarcodeFormat.QrCode, message.Format);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("Hello, structured world"), message.RawBytes.ToArray());
        CollectionAssert.AreEqual(symbols, message.Segments.ToArray());
        Assert.IsEmpty(assembler.Pending);
    }

    [TestMethod]
    public void ASymbolScannedTwiceIsCountedOnce()
    {
        var symbols = QrSequence("ONE TWO", "ONE ", "TWO");
        var again = Qr("ONE ", 0, 2, Parity("ONE TWO"));
        var assembler = new BbStructuredAppendAssembler();

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(symbols[0], out _));
        Assert.AreEqual(BbStructuredAppendStatus.Duplicate, assembler.Add(again, out _));
        Assert.AreEqual(1, assembler.Pending.Single().Received);
        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(symbols[1], out var message));
        Assert.AreEqual("ONE TWO", message!.Text);
    }

    [TestMethod]
    public void AQrSequenceWhoseParityDoesNotMatchIsRejected()
    {
        var wrong = Parity("ONE TWO") ^ 0x55;
        var assembler = new BbStructuredAppendAssembler();

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(Qr("ONE ", 0, 2, wrong), out _));
        Assert.AreEqual(BbStructuredAppendStatus.Rejected, assembler.Add(Qr("TWO", 1, 2, wrong), out var message));
        Assert.IsNull(message);
        Assert.IsEmpty(assembler.Pending, "A rejected sequence is dropped.");
    }

    [TestMethod]
    public void SequencesScannedInterleavedAreKeptApart()
    {
        var first = QrSequence("FIRST-1", "FIRST", "-1");
        var second = QrSequence("SECOND-2", "SECOND", "-2");
        Assert.AreNotEqual(first[0].Metadata!.StructuredAppendParity, second[0].Metadata!.StructuredAppendParity);
        var assembler = new BbStructuredAppendAssembler();

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(first[0], out _));
        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(second[1], out _));
        Assert.HasCount(2, assembler.Pending);
        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(second[0], out var b));
        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(first[1], out var a));

        Assert.AreEqual("FIRST-1", a!.Text);
        Assert.AreEqual("SECOND-2", b!.Text);
    }

    [TestMethod]
    public void TwoDifferentSymbolsForOnePlaceAreNeverCombined()
    {
        // Same count, same parity, different first symbols: they cannot both belong to one message.
        const int parity = 0x41;
        var assembler = new BbStructuredAppendAssembler();

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(Qr("FIRST", 0, 2, parity), out _));
        Assert.AreEqual(BbStructuredAppendStatus.Conflict, assembler.Add(Qr("OTHER", 0, 2, parity), out _));

        var pending = assembler.Pending.Single();
        Assert.AreEqual(1, pending.Received, "The old sequence is dropped and the new one starts with the symbol that contradicted it.");
    }

    [TestMethod]
    public void ADataMatrixSequenceIsAssembledAndItsGapsNumberedFromOne()
    {
        var assembler = new BbStructuredAppendAssembler();

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(DataMatrix("C3", 3, 3, 0x0102), out _));
        CollectionAssert.AreEqual(new[] { 1, 2 }, assembler.Pending.Single().Missing.ToArray());
        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(DataMatrix("A1", 1, 3, 0x0102), out _));
        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(DataMatrix("ELSEWHERE", 1, 3, 0x0103), out _));
        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(DataMatrix("B2", 2, 3, 0x0102), out var message));

        Assert.AreEqual("A1B2C3", message!.Text);
        Assert.AreEqual(BbBarcodeFormat.DataMatrix, assembler.Pending.Single().Format, "The other file's symbol still waits.");
    }

    [TestMethod]
    public void AUtf8CharacterSplitBetweenSymbolsIsDecodedWhole()
    {
        // "Grüße" in UTF-8, cut between the two bytes of the ü: each symbol on its own holds half a character.
        var bytes = Encoding.UTF8.GetBytes("Grüße");
        var first = Encoding.Latin1.GetString(bytes, 0, 3);
        var second = Encoding.Latin1.GetString(bytes, 3, bytes.Length - 3);
        var assembler = new BbStructuredAppendAssembler();

        var a = DataMatrix(first, 1, 2, 7, eci: 26);
        var b = DataMatrix(second, 2, 2, 7, eci: 26);
        Assert.Contains('\uFFFD', a.Text);

        assembler.Add(a, out _);
        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(b, out var message));
        Assert.AreEqual("Grüße", message!.Text);
    }

    [TestMethod]
    public void AMacroPdf417FileWithoutACountIsCompleteOnceItsLastSegmentAndEveryOneBeforeItAreRead()
    {
        var assembler = new BbStructuredAppendAssembler();

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(Pdf417("ONE", 0, FileA), out _));
        var pending = assembler.Pending.Single();
        Assert.IsNull(pending.Count, "Until the last segment is read nobody knows how many there are.");
        Assert.IsEmpty(pending.Missing);

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(Pdf417("THREE", 2, FileA, last: true), out _));
        pending = assembler.Pending.Single();
        Assert.AreEqual(3, pending.Count);
        CollectionAssert.AreEqual(new[] { 1 }, pending.Missing.ToArray());

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(Pdf417("OTHER FILE", 1, FileB), out _));
        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(Pdf417("TWO", 1, FileA), out var message));
        Assert.AreEqual("ONETWOTHREE", message!.Text);
        Assert.AreEqual(BbBarcodeFormat.Pdf417, message.Format);
    }

    [TestMethod]
    public void AMacroPdf417FileWithACountIsCompleteWithoutItsTerminator()
    {
        var assembler = new BbStructuredAppendAssembler();

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(Pdf417("B", 1, FileA, count: 2), out _));
        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(Pdf417("A", 0, FileA, count: 2), out var message));
        Assert.AreEqual("AB", message!.Text);
    }

    [TestMethod]
    public void AMacroPdf417SegmentThatContradictsItsFileEndsIt()
    {
        var assembler = new BbStructuredAppendAssembler();
        assembler.Add(Pdf417("LAST", 1, FileA, last: true), out _);

        Assert.AreEqual(BbStructuredAppendStatus.Conflict, assembler.Add(Pdf417("AFTER THE LAST", 2, FileA), out _));

        var sizes = new BbStructuredAppendAssembler();
        sizes.Add(Pdf417("A", 0, FileA, count: 3), out _);
        Assert.AreEqual(BbStructuredAppendStatus.Conflict, sizes.Add(Pdf417("B", 1, FileA, count: 4), out _));
    }

    [TestMethod]
    public void ASegmentThatContradictsItselfStartsNothing()
    {
        var assembler = new BbStructuredAppendAssembler();

        // Marked last, at the second of three places.
        Assert.AreEqual(BbStructuredAppendStatus.Rejected, assembler.Add(Pdf417("ODD", 1, FileA, count: 3, last: true), out _));
        Assert.IsEmpty(assembler.Pending);
    }

    [TestMethod]
    public void ASegmentBeyondTheLimitIsRejected()
    {
        var assembler = new BbStructuredAppendAssembler { MaxSegments = 10 };

        Assert.AreEqual(BbStructuredAppendStatus.Rejected, assembler.Add(Pdf417("FAR", 10, FileA), out _));
        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(Pdf417("NEAR", 9, FileA), out _));
    }

    [TestMethod]
    public void ASequenceThatWaitsTooLongIsDropped()
    {
        var clock = new ManualClock();
        var assembler = new BbStructuredAppendAssembler(clock) { Timeout = TimeSpan.FromSeconds(30) };
        var symbols = QrSequence("ONE TWO", "ONE ", "TWO");

        assembler.Add(symbols[0], out _);
        clock.Advance(TimeSpan.FromSeconds(29));
        Assert.HasCount(1, assembler.Pending);

        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.IsEmpty(assembler.Pending);
        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(symbols[1], out var message), "The first symbol is forgotten.");
        Assert.IsNull(message);
    }

    [TestMethod]
    public void ARepeatedSymbolKeepsItsSequenceAlive()
    {
        var clock = new ManualClock();
        var assembler = new BbStructuredAppendAssembler(clock) { Timeout = TimeSpan.FromSeconds(30) };
        var symbols = QrSequence("ONE TWO", "ONE ", "TWO");

        assembler.Add(symbols[0], out _);
        clock.Advance(TimeSpan.FromSeconds(20));
        assembler.Add(symbols[0], out _);
        clock.Advance(TimeSpan.FromSeconds(20));

        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(symbols[1], out _));
    }

    [TestMethod]
    public void TheSequenceWaitingLongestMakesWayWhenTheLimitIsReached()
    {
        var clock = new ManualClock();
        var assembler = new BbStructuredAppendAssembler(clock) { MaxPendingSequences = 2 };

        assembler.Add(DataMatrix("A", 1, 2, 1), out _);
        clock.Advance(TimeSpan.FromSeconds(1));
        assembler.Add(DataMatrix("B", 1, 2, 2), out _);
        clock.Advance(TimeSpan.FromSeconds(1));
        assembler.Add(DataMatrix("C", 1, 2, 3), out _);

        Assert.HasCount(2, assembler.Pending);
        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(DataMatrix("A2", 2, 2, 1), out _), "File 1 was pushed out.");
        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(DataMatrix("C2", 2, 2, 3), out var message));
        Assert.AreEqual("CC2", message!.Text);
    }

    [TestMethod]
    public void ResetForgetsEverySequence()
    {
        var assembler = new BbStructuredAppendAssembler();
        assembler.Add(DataMatrix("A", 1, 2, 1), out _);

        assembler.Reset();

        Assert.IsEmpty(assembler.Pending);
    }

    [TestMethod]
    public void AnImpossibleHeaderIsRejectedRatherThanTakenForAWholeMessage()
    {
        var assembler = new BbStructuredAppendAssembler();

        // Place 5 of 3: a fragment of something, but not of anything that can be put together.
        Assert.AreEqual(BbStructuredAppendStatus.Rejected, assembler.Add(Qr("FRAGMENT", 5, 3, 0), out var message));
        Assert.IsNull(message);
        Assert.AreEqual(BbStructuredAppendStatus.Rejected, assembler.Add(Pdf417("FRAGMENT", 5, FileA, count: 3), out _));
        Assert.IsEmpty(assembler.Pending);
    }

    [TestMethod]
    public void AMacroPdf417SegmentWithoutAFileIdIsRejected()
    {
        // Without an ID nothing tells one file's segments from another's.
        var segment = Pdf417("NO ID", 0, []);
        var assembler = new BbStructuredAppendAssembler();

        Assert.AreEqual(0, segment.Metadata!.StructuredAppendIndex);
        Assert.IsNull(segment.Metadata.StructuredAppendFileId);
        Assert.AreEqual(BbStructuredAppendStatus.Rejected, assembler.Add(segment, out _));
    }

    [TestMethod]
    public void SymbolsStillInViewAfterTheMessageIsReturnedDoNotReturnItAgain()
    {
        var clock = new ManualClock();
        var assembler = new BbStructuredAppendAssembler(clock) { Timeout = TimeSpan.FromSeconds(30) };
        var symbols = QrSequence("ONE TWO", "ONE ", "TWO");

        assembler.Add(symbols[0], out _);
        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(symbols[1], out _));

        // A camera keeps reporting what it sees.
        Assert.AreEqual(BbStructuredAppendStatus.Duplicate, assembler.Add(symbols[1], out var message));
        Assert.IsNull(message);
        clock.Advance(TimeSpan.FromSeconds(20));
        Assert.AreEqual(BbStructuredAppendStatus.Duplicate, assembler.Add(symbols[0], out _));
        Assert.IsEmpty(assembler.Pending);

        // Out of view for the timeout, the message can be read again.
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(symbols[0], out _));
    }

    [TestMethod]
    public void ResetLetsACompletedMessageBeReadAgain()
    {
        var assembler = new BbStructuredAppendAssembler();
        var symbols = QrSequence("ONE TWO", "ONE ", "TWO");
        assembler.Add(symbols[0], out _);
        assembler.Add(symbols[1], out _);

        assembler.Reset();

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(symbols[0], out _));
    }

    [TestMethod]
    public void AnotherMessageWithTheSameIdentificationIsNotTakenForTheFinishedOne()
    {
        var assembler = new BbStructuredAppendAssembler();
        assembler.Add(DataMatrix("A1", 1, 2, 9), out _);
        assembler.Add(DataMatrix("A2", 2, 2, 9), out _);

        Assert.AreEqual(BbStructuredAppendStatus.Added, assembler.Add(DataMatrix("B1", 1, 2, 9), out _));
        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(DataMatrix("B2", 2, 2, 9), out var message));
        Assert.AreEqual("B1B2", message!.Text);
    }

    [TestMethod]
    public void AUtf8CharacterSplitBetweenSymbolsWithoutAnEciIsDecodedWhole()
    {
        // No ECI: each half guesses Latin-1 on its own, the bytes together are valid UTF-8.
        var bytes = Encoding.UTF8.GetBytes("Grüße");
        var assembler = new BbStructuredAppendAssembler();

        var a = DataMatrix(Encoding.Latin1.GetString(bytes, 0, 3), 1, 2, 8);
        var b = DataMatrix(Encoding.Latin1.GetString(bytes, 3, bytes.Length - 3), 2, 2, 8);
        Assert.AreEqual("GrÃ", a.Text, "On its own, the first half of the ü reads as Latin-1.");

        assembler.Add(a, out _);
        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(b, out var message));
        Assert.AreEqual("Grüße", message!.Text);
    }

    [TestMethod]
    public void ASymbolOutsideASequenceIsLeftAlone()
    {
        var plain = Decode(QrEncoder.Encode("PLAIN"), BbBarcodeFormat.QrCode);
        var assembler = new BbStructuredAppendAssembler();

        Assert.AreEqual(BbStructuredAppendStatus.NotStructuredAppend, assembler.Add(plain, out var message));
        Assert.IsNull(message);
        Assert.IsEmpty(assembler.Pending);
    }

    [TestMethod]
    public void ASingleSymbolSequenceCompletesAtOnce()
    {
        var assembler = new BbStructuredAppendAssembler();

        Assert.AreEqual(BbStructuredAppendStatus.Completed, assembler.Add(Qr("ALONE", 0, 1, Parity("ALONE")), out var message));
        Assert.AreEqual("ALONE", message!.Text);
    }

    [TestMethod]
    public void TheLimitsAreValidated()
    {
        var assembler = new BbStructuredAppendAssembler();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => assembler.Timeout = TimeSpan.Zero);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => assembler.MaxPendingSequences = 0);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => assembler.MaxSegments = 1);
        assembler.Timeout = Timeout.InfiniteTimeSpan;
        Assert.ThrowsExactly<ArgumentNullException>(() => assembler.Add(null!, out _));
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
