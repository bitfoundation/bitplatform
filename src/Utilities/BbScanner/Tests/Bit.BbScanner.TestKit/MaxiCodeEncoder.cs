namespace Bit.BbScanner.TestKit;

/// <summary>
/// A minimal MaxiCode encoder (ISO/IEC 16023): a standard symbol (mode 4) or one with enhanced error correction
/// (mode 5), its message in code sets A and B with latches between them and numeric shifts for runs of nine digits.
/// The structured carrier message of modes 2 and 3 is not written; the reference fixtures carry it.
/// </summary>
public static class MaxiCodeEncoder
{
    /// <summary>Where each bit of the codewords lies on the 30 by 33 grid, as <c>row * 30 + column</c>, as BWIPP draws it.</summary>
    private static readonly short[] ModuleMap =
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

    /// <summary>The modules that are dark in every symbol: the orientation patterns and the two in the top right corner.</summary>
    private static readonly short[] FixedDark = [28, 29, 280, 281, 311, 457, 488, 500, 530, 670, 700, 677, 707];

    /// <summary>Code sets A and B: the character each codeword stands for, or a negative control code.</summary>
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
    ];

    private const int Pad = 33;
    private const int NumericShift = 31;
    private const int Latch = 63; // latch B in code set A, latch A in code set B

    /// <summary>Encodes <paramref name="content"/>, characters of code sets A and B only, in mode 4 or 5.</summary>
    public static BbBitMatrix Encode(string content, int mode = 4)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentOutOfRangeException.ThrowIfLessThan(mode, 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mode, 5);

        var message = new List<int>();
        var set = 0;
        for (var i = 0; i < content.Length;)
        {
            var digits = 0;
            while (i + digits < content.Length && char.IsAsciiDigit(content[i + digits]))
            {
                digits++;
            }

            if (digits >= 9)
            {
                // Nine digits in five codewords of six bits.
                var value = int.Parse(content.AsSpan(i, 9), System.Globalization.CultureInfo.InvariantCulture);
                message.Add(NumericShift);
                for (var shift = 24; shift >= 0; shift -= 6)
                {
                    message.Add((value >> shift) & 0x3F);
                }

                i += 9;
                continue;
            }

            var code = Array.IndexOf(Sets[set], (short)content[i]);
            if (code < 0)
            {
                var other = Array.IndexOf(Sets[1 - set], (short)content[i]);
                if (other < 0)
                {
                    throw new ArgumentException($"'{content[i]}' is in neither code set A nor B.", nameof(content));
                }

                message.Add(Latch);
                set = 1 - set;
                code = other;
            }

            message.Add(code);
            i++;
        }

        var dataLength = mode == 4 ? 93 : 77;
        if (message.Count > dataLength)
        {
            throw new ArgumentException("The message is too long for one MaxiCode symbol.", nameof(content));
        }

        while (message.Count < dataLength)
        {
            message.Add(Pad);
        }

        // The primary message: the mode and the first nine codewords, and ten check codewords; the secondary message the
        // rest, its even and odd codewords two blocks with half the check codewords each.
        var (secondaryData, secondaryCheck) = mode == 4 ? (84, 40) : (68, 56);
        var codewords = new int[144];
        var primary = new int[20];
        primary[0] = mode;
        for (var i = 0; i < 9; i++)
        {
            primary[i + 1] = message[i];
        }

        var encoder = new ReedSolomonEncoder(new BbGenericGF(0x43, 64, 1));
        encoder.Encode(primary, 10);
        primary.CopyTo(codewords, 0);
        for (var half = 0; half < 2; half++)
        {
            var block = new int[(secondaryData + secondaryCheck) / 2];
            for (var i = 0; i < secondaryData / 2; i++)
            {
                block[i] = message[9 + (2 * i) + half];
            }

            encoder.Encode(block, secondaryCheck / 2);
            for (var i = 0; i < block.Length; i++)
            {
                codewords[20 + (2 * i) + half] = block[i];
            }
        }

        var matrix = new BbBitMatrix(30, 33);
        for (var bit = 0; bit < ModuleMap.Length; bit++)
        {
            if (((codewords[bit / 6] >> (5 - (bit % 6))) & 1) == 1)
            {
                matrix[ModuleMap[bit] % 30, ModuleMap[bit] / 30] = true;
            }
        }

        foreach (var module in FixedDark)
        {
            matrix[module % 30, module / 30] = true;
        }

        return matrix;
    }
}
