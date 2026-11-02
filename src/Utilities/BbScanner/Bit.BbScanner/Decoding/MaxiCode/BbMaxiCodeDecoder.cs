using System.Globalization;
using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Turns the 144 codewords of a MaxiCode symbol (ISO/IEC 16023) into its message: Reed-Solomon correction of the
/// primary and the two interleaved halves of the secondary message, then the code sets.
/// </summary>
/// <remarks>
/// <para>
/// A codeword is six bits. The first ten, and the ten check codewords after them, are the primary message, corrected
/// on their own; its first codeword's low four bits are the mode. Modes 2, 3 and 4 use standard error correction for
/// the secondary message - 84 data codewords and 40 check codewords - and mode 5 enhanced error correction - 68 and 56
/// - and either way the secondary message is two Reed-Solomon blocks, its even and its odd codewords. Mode 6, which
/// programs a reader rather than carrying data for an application, is not read.
/// </para>
/// <para>
/// Modes 2 and 3 are the structured carrier message on parcels: the primary message holds a postal code - up to nine
/// digits in mode 2, six characters in mode 3 - a three digit country code and a three digit service class, and the
/// secondary message the rest. They are reported the way the carrier writes them, as one message: after the header
/// <c>[)&gt;</c> RS <c>01</c> GS and the two digit year when the secondary message starts with it, at the start
/// otherwise, separated by group separators.
/// </para>
/// <para>
/// The message is in five code sets of 64 characters each, A to E, with latches, shifts for one, two or three
/// characters, locks, a numeric shift that packs nine digits into five codewords, and ECI designators. Its characters
/// are bytes - ISO/IEC 8859-1 unless an ECI says otherwise, or UTF-8 when they are valid UTF-8 and none does.
/// </para>
/// </remarks>
internal static class BbMaxiCodeDecoder
{
    /// <summary>The grid a MaxiCode symbol's modules sit on: 33 rows of 30, every odd row half a module to the right.</summary>
    public const int Columns = 30;

    /// <inheritdoc cref="Columns"/>
    public const int Rows = 33;

    /// <summary>The codewords of a symbol: ten primary data, ten primary check and 124 secondary.</summary>
    public const int Codewords = 144;

    private static readonly BbReedSolomonDecoder Correction = new(new BbGenericGF(0x43, 64, 1));

    /// <summary>
    /// Where each bit of the codewords lies on the grid, as <c>row * 30 + column</c>: bit <c>6 k + b</c> is bit
    /// <c>5 - b</c> of codeword <c>k</c>, the most significant first. The layout the specification draws, as BWIPP
    /// renders it.
    /// </summary>
    internal static ReadOnlySpan<short> ModuleMap =>
    [
        469, 529, 286, 316, 347, 346, 673, 672, 703, 702, 647, 676, 283, 282, 313, 312, 370, 610, 618, 379, 378, 409, 408, 439,
        705, 704, 559, 589, 588, 619, 458, 518, 640, 701, 675, 674, 285, 284, 315, 314, 310, 340, 531, 289, 288, 319, 349, 348,
        456, 486, 517, 516, 471, 470, 369, 368, 399, 398, 429, 428, 549, 548, 579, 578, 609, 608, 649, 648, 679, 678, 709, 708,
        639, 638, 669, 668, 699, 698, 279, 278, 309, 308, 339, 338, 381, 380, 411, 410, 441, 440, 561, 560, 591, 590, 621, 620,
        547, 546, 577, 576, 607, 606, 367, 366, 397, 396, 427, 426, 291, 290, 321, 320, 351, 350, 651, 650, 681, 680, 711, 710,
        1, 0, 31, 30, 61, 60, 3, 2, 33, 32, 63, 62, 5, 4, 35, 34, 65, 64, 7, 6, 37, 36, 67, 66,
        9, 8, 39, 38, 69, 68, 11, 10, 41, 40, 71, 70, 13, 12, 43, 42, 73, 72, 15, 14, 45, 44, 75, 74,
        17, 16, 47, 46, 77, 76, 19, 18, 49, 48, 79, 78, 21, 20, 51, 50, 81, 80, 23, 22, 53, 52, 83, 82,
        25, 24, 55, 54, 85, 84, 27, 26, 57, 56, 87, 86, 117, 116, 147, 146, 177, 176, 115, 114, 145, 144, 175, 174,
        113, 112, 143, 142, 173, 172, 111, 110, 141, 140, 171, 170, 109, 108, 139, 138, 169, 168, 107, 106, 137, 136, 167, 166,
        105, 104, 135, 134, 165, 164, 103, 102, 133, 132, 163, 162, 101, 100, 131, 130, 161, 160, 99, 98, 129, 128, 159, 158,
        97, 96, 127, 126, 157, 156, 95, 94, 125, 124, 155, 154, 93, 92, 123, 122, 153, 152, 91, 90, 121, 120, 151, 150,
        181, 180, 211, 210, 241, 240, 183, 182, 213, 212, 243, 242, 185, 184, 215, 214, 245, 244, 187, 186, 217, 216, 247, 246,
        189, 188, 219, 218, 249, 248, 191, 190, 221, 220, 251, 250, 193, 192, 223, 222, 253, 252, 195, 194, 225, 224, 255, 254,
        197, 196, 227, 226, 257, 256, 199, 198, 229, 228, 259, 258, 201, 200, 231, 230, 261, 260, 203, 202, 233, 232, 263, 262,
        205, 204, 235, 234, 265, 264, 207, 206, 237, 236, 267, 266, 297, 296, 327, 326, 357, 356, 295, 294, 325, 324, 355, 354,
        293, 292, 323, 322, 353, 352, 277, 276, 307, 306, 337, 336, 275, 274, 305, 304, 335, 334, 273, 272, 303, 302, 333, 332,
        271, 270, 301, 300, 331, 330, 361, 360, 391, 390, 421, 420, 363, 362, 393, 392, 423, 422, 365, 364, 395, 394, 425, 424,
        383, 382, 413, 412, 443, 442, 385, 384, 415, 414, 445, 444, 387, 386, 417, 416, 447, 446, 477, 476, 507, 506, 537, 536,
        475, 474, 505, 504, 535, 534, 473, 472, 503, 502, 533, 532, 455, 454, 485, 484, 515, 514, 453, 452, 483, 482, 513, 512,
        451, 450, 481, 480, 511, 510, 541, 540, 571, 570, 601, 600, 543, 542, 573, 572, 603, 602, 545, 544, 575, 574, 605, 604,
        563, 562, 593, 592, 623, 622, 565, 564, 595, 594, 625, 624, 567, 566, 597, 596, 627, 626, 657, 656, 687, 686, 717, 716,
        655, 654, 685, 684, 715, 714, 653, 652, 683, 682, 713, 712, 637, 636, 667, 666, 697, 696, 635, 634, 665, 664, 695, 694,
        633, 632, 663, 662, 693, 692, 631, 630, 661, 660, 691, 690, 721, 720, 751, 750, 781, 780, 723, 722, 753, 752, 783, 782,
        725, 724, 755, 754, 785, 784, 727, 726, 757, 756, 787, 786, 729, 728, 759, 758, 789, 788, 731, 730, 761, 760, 791, 790,
        733, 732, 763, 762, 793, 792, 735, 734, 765, 764, 795, 794, 737, 736, 767, 766, 797, 796, 739, 738, 769, 768, 799, 798,
        741, 740, 771, 770, 801, 800, 743, 742, 773, 772, 803, 802, 745, 744, 775, 774, 805, 804, 747, 746, 777, 776, 807, 806,
        837, 836, 867, 866, 897, 896, 835, 834, 865, 864, 895, 894, 833, 832, 863, 862, 893, 892, 831, 830, 861, 860, 891, 890,
        829, 828, 859, 858, 889, 888, 827, 826, 857, 856, 887, 886, 825, 824, 855, 854, 885, 884, 823, 822, 853, 852, 883, 882,
        821, 820, 851, 850, 881, 880, 819, 818, 849, 848, 879, 878, 817, 816, 847, 846, 877, 876, 815, 814, 845, 844, 875, 874,
        813, 812, 843, 842, 873, 872, 811, 810, 841, 840, 871, 870, 901, 900, 931, 930, 961, 960, 903, 902, 933, 932, 963, 962,
        905, 904, 935, 934, 965, 964, 907, 906, 937, 936, 967, 966, 909, 908, 939, 938, 969, 968, 911, 910, 941, 940, 971, 970,
        913, 912, 943, 942, 973, 972, 915, 914, 945, 944, 975, 974, 917, 916, 947, 946, 977, 976, 919, 918, 949, 948, 979, 978,
        921, 920, 951, 950, 981, 980, 923, 922, 953, 952, 983, 982, 925, 924, 955, 954, 985, 984, 927, 926, 957, 956, 987, 986,
        58, 89, 88, 118, 149, 148, 178, 209, 208, 238, 269, 268, 298, 329, 328, 358, 389, 388, 418, 449, 448, 478, 509, 508,
        538, 569, 568, 598, 629, 628, 658, 689, 688, 718, 749, 748, 778, 809, 808, 838, 869, 868, 898, 929, 928, 958, 989, 988,
    ];

    // Control codes in the code set tables: what a codeword stands for when it is not a character.
    private const int Eci = -1;
    private const int Pad = -2;
    private const int NumericShift = -3;
    private const int LatchA = -4;
    private const int LatchB = -5;
    private const int ShiftA = -6;
    private const int ShiftB = -7;
    private const int ShiftC = -8;
    private const int ShiftD = -9;
    private const int ShiftE = -10;
    private const int TwoShiftA = -11;
    private const int ThreeShiftA = -12;
    private const int LockC = -13;
    private const int LockD = -14;
    private const int LockE = -15;

    /// <summary>The five code sets, A to E: the character each codeword stands for in each, or a control code.</summary>
    private static readonly short[][] Sets =
    [
        // Code set A.
        [
            13, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
            80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, -1, 28, 29, 30, -3,
            32, -2, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47,
            48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, -7, -8, -9, -10, -5,
        ],
        // Code set B.
        [
            96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111,
            112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, -1, 28, 29, 30, -3,
            123, -2, 125, 126, 127, 59, 60, 61, 62, 63, 91, 92, 93, 94, 95, 32,
            44, 46, 47, 58, 64, 33, 124, -2, -11, -12, -2, -6, -8, -9, -10, -4,
        ],
        // Code set C.
        [
            192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207,
            208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, -1, 28, 29, 30, -3,
            219, 220, 221, 222, 223, 170, 172, 177, 178, 179, 181, 185, 186, 188, 189, 190,
            128, 129, 130, 131, 132, 133, 134, 135, 136, 137, -4, 32, -13, -9, -10, -5,
        ],
        // Code set D.
        [
            224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239,
            240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, -1, 28, 29, 30, -3,
            251, 252, 253, 254, 255, 161, 168, 171, 175, 176, 180, 183, 184, 187, 191, 138,
            139, 140, 141, 142, 143, 144, 145, 146, 147, 148, -4, 32, -8, -14, -10, -5,
        ],
        // Code set E.
        [
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, -1, -2, -2, 27, -3,
            28, 29, 30, 31, 159, 160, 162, 163, 164, 165, 166, 167, 169, 173, 174, 182,
            149, 150, 151, 152, 153, 154, 155, 156, 157, 158, -4, 32, -8, -9, -15, -5,
        ],
    ];

    /// <summary>
    /// Decodes a symbol's codewords, in the order <see cref="ModuleMap"/> numbers them; <see langword="null"/> when they
    /// cannot be corrected, or hold a mode this library does not read.
    /// </summary>
    public static BbDecoderResult? Decode(ReadOnlySpan<int> codewords)
    {
        if (codewords.Length != Codewords)
        {
            return null;
        }

        var words = codewords.ToArray();
        if (!Correct(words, 0, 10, 10, 0, out var corrected))
        {
            return null;
        }

        var mode = words[0] & 0x0F;
        var (data, check) = mode switch
        {
            2 or 3 or 4 => (84, 40),
            5 => (68, 56),
            _ => (0, 0),
        };
        if (data == 0 ||
            !Correct(words, 20, data, check, 1, out var even) ||
            !Correct(words, 20, data, check, 2, out var odd))
        {
            return null;
        }

        // The primary message's ten data codewords, then the secondary message's.
        var message = new int[10 + data];
        Array.Copy(words, 0, message, 0, 10);
        Array.Copy(words, 20, message, 10, data);

        var output = new Output();
        if (mode is 2 or 3)
        {
            var postcode = mode == 2 ? PostCode2(message) : PostCode3(message);
            if (postcode is null)
            {
                return null;
            }

            ReadMessage(message, 10, data, output);
            var text = output.Text;
            var carrier = $"{postcode}\u001d{Country(message):D3}\u001d{ServiceClass(message):D3}\u001d";
            const string Header = "[)>\u001e01\u001d";
            text = text.StartsWith(Header, StringComparison.Ordinal) && text.Length >= Header.Length + 2
                ? text.Insert(Header.Length + 2, carrier)
                : carrier + text;
            return Result(Encoding.UTF8.GetBytes(text), text, mode, corrected + even + odd, output.Eci);
        }

        ReadMessage(message, 1, message.Length - 1, output);
        return Result(output.Raw.ToArray(), output.Text, mode, corrected + even + odd, output.Eci);
    }

    private static BbDecoderResult Result(byte[] bytes, string text, int mode, int errorsCorrected, int? eci) =>
        new(bytes, text)
        {
            SymbolVersion = string.Create(CultureInfo.InvariantCulture, $"Mode {mode}"),
            ErrorCorrectionLevel = mode == 5 ? "Enhanced" : "Standard",
            ErrorsCorrected = errorsCorrected,
            Eci = eci,
        };

    /// <summary>
    /// Corrects one Reed-Solomon block in place: the primary message whole (<paramref name="half"/> 0), or the even (1)
    /// or odd (2) codewords of the secondary message from <paramref name="start"/>.
    /// </summary>
    private static bool Correct(int[] words, int start, int data, int check, int half, out int corrected)
    {
        var divisor = half == 0 ? 1 : 2;
        var block = new int[(data + check) / divisor];
        for (var i = 0; i < data + check; i++)
        {
            if (half == 0 || (i % 2) == half - 1)
            {
                block[i / divisor] = words[start + i];
            }
        }

        if (!Correction.TryDecode(block, check / divisor, out corrected))
        {
            return false;
        }

        for (var i = 0; i < data; i++)
        {
            if (half == 0 || (i % 2) == half - 1)
            {
                words[start + i] = block[i / divisor];
            }
        }

        return true;
    }

    /// <summary>The bits <paramref name="positions"/>, numbered from 1 across the primary message, as a number.</summary>
    private static int Bits(int[] message, ReadOnlySpan<byte> positions)
    {
        var value = 0;
        foreach (var position in positions)
        {
            var bit = position - 1;
            value = (value << 1) | ((message[bit / 6] >> (5 - (bit % 6))) & 1);
        }

        return value;
    }

    private static int Country(int[] message) => Bits(message, [53, 54, 43, 44, 45, 46, 47, 48, 37, 38]);

    private static int ServiceClass(int[] message) => Bits(message, [55, 56, 57, 58, 59, 60, 49, 50, 51, 52]);

    /// <summary>Mode 2's postal code: up to nine digits, as many as its length says.</summary>
    private static string? PostCode2(int[] message)
    {
        var length = Bits(message, [39, 40, 41, 42, 31, 32]);
        var value = Bits(message, [33, 34, 35, 36, 25, 26, 27, 28, 29, 30, 19, 20, 21, 22, 23, 24, 13, 14, 15, 16, 17, 18, 7, 8, 9, 10, 11, 12, 1, 2]);
        var digits = value.ToString(CultureInfo.InvariantCulture);
        return length is < 1 or > 9 || digits.Length > length ? null : digits.PadLeft(length, '0');
    }

    /// <summary>Mode 3's postal code: six characters of code set A, the spaces it is padded with at the end trimmed.</summary>
    private static string? PostCode3(int[] message)
    {
        Span<char> characters = stackalloc char[6];
        ReadOnlySpan<byte> positions =
        [
            39, 40, 41, 42, 31, 32, 33, 34, 35, 36, 25, 26, 27, 28, 29, 30, 19, 20,
            21, 22, 23, 24, 13, 14, 15, 16, 17, 18, 7, 8, 9, 10, 11, 12, 1, 2,
        ];
        for (var i = 0; i < 6; i++)
        {
            var value = Sets[0][Bits(message, positions.Slice(6 * i, 6))];
            if (value < 32)
            {
                return null;
            }

            characters[i] = (char)value;
        }

        return new string(characters).TrimEnd(' ');
    }

    /// <summary>Reads <paramref name="count"/> codewords of message from <paramref name="start"/> through the code sets.</summary>
    private static void ReadMessage(int[] message, int start, int count, Output output)
    {
        var set = 0;
        var saved = 0;
        var shift = -1;
        var end = start + count;
        for (var i = start; i < end; i++)
        {
            var value = Sets[set][message[i]];
            switch (value)
            {
                case LatchA:
                    (set, shift) = (0, -1);
                    break;
                case LatchB:
                    (set, shift) = (1, -1);
                    break;
                case ShiftA or ShiftB or ShiftC or ShiftD or ShiftE:
                    (saved, set, shift) = (set, ShiftA - value, 1);
                    break;
                case TwoShiftA:
                    (saved, set, shift) = (set, 0, 2);
                    break;
                case ThreeShiftA:
                    (saved, set, shift) = (set, 0, 3);
                    break;
                case LockC or LockD or LockE:
                    shift = -1;
                    break;
                case NumericShift:
                    if (i + 5 >= end)
                    {
                        return;
                    }

                    var digits = (message[i + 1] << 24) | (message[i + 2] << 18) | (message[i + 3] << 12) | (message[i + 4] << 6) | message[i + 5];
                    output.Append(digits.ToString("D9", CultureInfo.InvariantCulture));
                    i += 5;
                    break;
                case Eci:
                    i = ReadEci(message, i + 1, end, output) - 1;
                    break;
                case Pad:
                    break;
                default:
                    output.Append((byte)value);
                    break;
            }

            // A shift lasts its count of characters, then the set it shifted from returns.
            if (shift > 0 && value is not (ShiftA or ShiftB or ShiftC or ShiftD or ShiftE or TwoShiftA or ThreeShiftA) && --shift == 0)
            {
                (set, shift) = (saved, -1);
            }
        }
    }

    /// <summary>
    /// Reads the ECI designator from <paramref name="at"/>: one codeword up to 31, two whose first starts 10 up to
    /// 1 023, three whose first starts 110 up to 32 767, four whose first starts 1110 up to 999 999. Returns where the
    /// message goes on.
    /// </summary>
    private static int ReadEci(int[] message, int at, int end, Output output)
    {
        if (at >= end)
        {
            return end;
        }

        var first = message[at];
        var (extra, value) = first switch
        {
            < 32 => (0, first),
            < 48 => (1, first & 0x0F),
            < 56 => (2, first & 0x07),
            _ => (3, first & 0x03),
        };
        if (at + extra >= end)
        {
            return end;
        }

        for (var i = 1; i <= extra; i++)
        {
            value = (value << 6) | message[at + i];
        }

        output.SetEci(value);
        return at + extra + 1;
    }

    /// <summary>
    /// The decoded bytes, and the text they make in the character set in force where each was written: ISO-8859-1
    /// unless an ECI says otherwise, or UTF-8 when the bytes are valid UTF-8 and no ECI was given.
    /// </summary>
    private sealed class Output
    {
        private readonly StringBuilder _text = new();
        private readonly List<byte> _segment = [];
        private int? _segmentEci;

        public List<byte> Raw { get; } = [];

        public int? Eci { get; private set; }

        public string Text
        {
            get
            {
                Flush();
                return _text.ToString();
            }
        }

        public void Append(byte value)
        {
            Raw.Add(value);
            _segment.Add(value);
        }

        public void Append(string characters)
        {
            foreach (var character in characters)
            {
                Append((byte)character);
            }
        }

        public void SetEci(int eci)
        {
            Flush();
            Eci ??= eci;
            _segmentEci = eci <= 899 ? eci : null;
        }

        private void Flush()
        {
            if (_segment.Count == 0)
            {
                return;
            }

            var bytes = _segment.ToArray();
            var encoding = _segmentEci is int eci
                ? BbCharacterSetEci.FromEci(eci)
                : BbCharacterSetEci.GuessEncoding(bytes);
            _text.Append(encoding.GetString(bytes));
            _segment.Clear();
        }
    }
}
