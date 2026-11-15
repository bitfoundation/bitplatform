namespace Bit.Bcrdt.Tests;

/// <summary>
/// <see cref="BcrdtOrderKey"/>: every generated key sorts strictly inside its gap, and keys stay short.
/// </summary>
[TestClass]
public class OrderKeyTests
{
    public static IEnumerable<object[]> Seeds
    {
        get
        {
            for (int seed = 1; seed <= 25; seed++) yield return [seed];
        }
    }

    private static readonly string Smallest = "A" + new string('0', 26);
    private static readonly string Largest = "z" + new string('z', 26);

    [TestMethod]
    [DataRow(null, null, "a0")]
    [DataRow(null, "a0", "Zz")]
    [DataRow(null, "Zz", "Zy")]
    [DataRow("a0", null, "a1")]
    [DataRow("a1", null, "a2")]
    [DataRow("a0", "a1", "a0V")]
    [DataRow("a1", "a2", "a1V")]
    [DataRow("a0V", "a1", "a0l")]
    [DataRow("Zz", "a0", "ZzV")]
    [DataRow("Zz", "a1", "a0")]
    [DataRow(null, "Y00", "Xzzz")]
    [DataRow("bzz", null, "c000")]
    [DataRow("a0", "a0V", "a0G")]
    [DataRow("a0", "a0G", "a08")]
    [DataRow("b125", "b129", "b127")]
    [DataRow("a0", "a1V", "a1")]
    [DataRow("Zz", "a01", "a0")]
    [DataRow(null, "a0V", "a0")]
    [DataRow(null, "b999", "b99")]
    [DataRow("zzzzzzzzzzzzzzzzzzzzzzzzzzy", null, "zzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    [DataRow("zzzzzzzzzzzzzzzzzzzzzzzzzzz", null, "zzzzzzzzzzzzzzzzzzzzzzzzzzzV")]
    [DataRow(null, "A000000000000000000000000001", "A000000000000000000000000000V")]
    [DataRow(null, "A00000000000000000000000001", "A00000000000000000000000000V")]
    public void Keys_match_the_fractional_indexing_reference(string? before, string? after, string expected)
    {
        Assert.AreEqual(expected, BcrdtOrderKey.Between(before, after));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("a")]
    [DataRow("a00")]
    [DataRow("a0V0")]
    [DataRow("0")]
    [DataRow("1a")]
    [DataRow("a0!")]
    [DataRow("a0 ")]
    [DataRow("b0")]
    [DataRow("Z")]
    [DataRow("aé")]
    [DataRow("A000000000000000000000000000")]
    public void Malformed_keys_are_invalid_and_rejected(string key)
    {
        Assert.IsFalse(BcrdtOrderKey.IsValid(key));
        Assert.ThrowsExactly<ArgumentException>(() => BcrdtOrderKey.Between(key, null));
        Assert.ThrowsExactly<ArgumentException>(() => BcrdtOrderKey.Between(null, key));
        Assert.ThrowsExactly<ArgumentException>(() => BcrdtOrderKey.Between(null, key, new Random(1)));
        Assert.ThrowsExactly<ArgumentException>(() => BcrdtOrderKey.BetweenMany(key, null, 3));
    }

    [TestMethod]
    public void Well_formed_keys_are_valid()
    {
        foreach (string key in new[] { "a0", "Zz", "a0V", "b00", "b125", "A000000000000000000000000001", Largest, Largest + "V" })
        {
            Assert.IsTrue(BcrdtOrderKey.IsValid(key), key);
        }
        Assert.IsFalse(BcrdtOrderKey.IsValid(null));
        Assert.AreNotEqual(Smallest, BcrdtOrderKey.Between(null, Smallest + "1"));
    }

    [TestMethod]
    [DataRow("a1", "a0")]
    [DataRow("a0", "a0")]
    [DataRow("a0V", "a0G")]
    public void Bounds_out_of_order_are_rejected(string before, string after)
    {
        Assert.ThrowsExactly<ArgumentException>(() => BcrdtOrderKey.Between(before, after));
        Assert.ThrowsExactly<ArgumentException>(() => BcrdtOrderKey.Between(before, after, new Random(1)));
        Assert.ThrowsExactly<ArgumentException>(() => BcrdtOrderKey.BetweenMany(before, after, 2));
    }

    [TestMethod]
    public void Arguments_are_checked()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BcrdtOrderKey.Between(null, null, null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BcrdtOrderKey.BetweenMany(null, null, -1));
        Assert.IsEmpty(BcrdtOrderKey.BetweenMany("a0", "a1", 0));
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Every_key_sorts_strictly_inside_its_gap(int seed)
    {
        var random = new Random(seed);
        List<string> keys = [];

        for (int i = 0; i < 2000; i++)
        {
            int position = random.Next(keys.Count + 1);
            string? before = position > 0 ? keys[position - 1] : null;
            string? after = position < keys.Count ? keys[position] : null;

            string key = random.Next(2) == 0 ? BcrdtOrderKey.Between(before, after) : BcrdtOrderKey.Between(before, after, random);

            Assert.IsTrue(BcrdtOrderKey.IsValid(key), $"Seed {seed}: '{key}' is malformed.");
            if (before is not null) Assert.IsLessThan(0, string.CompareOrdinal(before, key), $"Seed {seed}: '{key}' is not after '{before}'.");
            if (after is not null) Assert.IsLessThan(0, string.CompareOrdinal(key, after), $"Seed {seed}: '{key}' is not before '{after}'.");
            keys.Insert(position, key);
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Many_keys_are_ascending_distinct_and_strictly_inside(int seed)
    {
        var random = new Random(seed);
        IReadOnlyList<string> pool = BcrdtOrderKey.BetweenMany(null, null, 50);

        for (int round = 0; round < 20; round++)
        {
            int low = random.Next(-1, pool.Count);
            int high = random.Next(low + 1, pool.Count + 1);
            string? before = low >= 0 ? pool[low] : null;
            string? after = high < pool.Count ? pool[high] : null;
            int count = random.Next(0, 400);

            IReadOnlyList<string> keys = BcrdtOrderKey.BetweenMany(before, after, count);

            Assert.HasCount(count, keys);
            for (int i = 0; i < keys.Count; i++)
            {
                Assert.IsTrue(BcrdtOrderKey.IsValid(keys[i]), $"Seed {seed}: '{keys[i]}' is malformed.");
                string? previous = i > 0 ? keys[i - 1] : before;
                if (previous is not null) Assert.IsLessThan(0, string.CompareOrdinal(previous, keys[i]), $"Seed {seed}: not ascending at {i}.");
            }
            if (count > 0 && after is not null) Assert.IsLessThan(0, string.CompareOrdinal(keys[^1], after), $"Seed {seed}: past the upper bound.");

            // Splitting the gap recursively keeps the keys logarithmic in the count.
            int bound = Math.Max(before?.Length ?? 0, after?.Length ?? 0) + 4;
            Assert.IsLessThanOrEqualTo(bound, keys.Count == 0 ? 0 : keys.Max(key => key.Length), $"Seed {seed}: keys grew too long.");
        }
    }

    [TestMethod]
    public void Ten_thousand_appends_or_prepends_keep_keys_short()
    {
        string? last = null;
        string? first = null;
        string? randomLast = null;
        string? randomFirst = null;
        var random = new Random(7);
        int longest = 0;
        int longestRandom = 0;

        for (int i = 0; i < 10_000; i++)
        {
            string appended = BcrdtOrderKey.Between(last, null);
            string prepended = BcrdtOrderKey.Between(null, first);
            string randomAppended = BcrdtOrderKey.Between(randomLast, null, random);
            string randomPrepended = BcrdtOrderKey.Between(null, randomFirst, random);

            if (last is not null) Assert.IsLessThan(0, string.CompareOrdinal(last, appended));
            if (first is not null) Assert.IsLessThan(0, string.CompareOrdinal(prepended, first));
            if (randomLast is not null) Assert.IsLessThan(0, string.CompareOrdinal(randomLast, randomAppended));
            if (randomFirst is not null) Assert.IsLessThan(0, string.CompareOrdinal(randomPrepended, randomFirst));

            (last, first, randomLast, randomFirst) = (appended, prepended, randomAppended, randomPrepended);
            longest = Math.Max(longest, Math.Max(appended.Length, prepended.Length));
            longestRandom = Math.Max(longestRandom, Math.Max(randomAppended.Length, randomPrepended.Length));
        }

        // The integer part gains a digit every power of 62: 10 000 steps need three digits. A random
        // key adds a fraction of a few characters on top, and the next append drops it again.
        Assert.IsLessThanOrEqualTo(4, longest);
        Assert.IsLessThanOrEqualTo(12, longestRandom);
    }

    [TestMethod]
    public void Inserting_at_one_place_over_and_over_lengthens_keys_by_a_fraction_of_a_character_each_time()
    {
        var random = new Random(5);

        foreach (bool randomized in new[] { false, true })
        {
            string low = "a0";
            string high = "a1";
            string middleLow = "a0";
            string middleHigh = "a1";
            int longest = 0;

            for (int i = 0; i < 1000; i++)
            {
                // Always just after the first element, and always in the middle of the list.
                high = randomized ? BcrdtOrderKey.Between(low, high, random) : BcrdtOrderKey.Between(low, high);
                string middle = randomized ? BcrdtOrderKey.Between(middleLow, middleHigh, random) : BcrdtOrderKey.Between(middleLow, middleHigh);
                if (i % 2 == 0) middleLow = middle;
                else middleHigh = middle;

                longest = Math.Max(longest, Math.Max(high.Length, middle.Length));
            }

            // A midpoint halves the gap, and a base-62 digit holds about six halvings; a random
            // point keeps a random share of it, which costs a little more on average.
            Assert.IsLessThanOrEqualTo(randomized ? 400 : 200, longest, $"randomized: {randomized}");
        }
    }

    [TestMethod]
    public void Random_keys_in_one_gap_rarely_collide()
    {
        var random = new Random(11);
        HashSet<string> keys = [];
        for (int i = 0; i < 1000; i++)
        {
            string key = BcrdtOrderKey.Between("a0", "a1", random);
            Assert.IsTrue(string.CompareOrdinal("a0", key) < 0 && string.CompareOrdinal(key, "a1") < 0);
            Assert.IsLessThanOrEqualTo(6, key.Length);
            keys.Add(key);
        }

        // 1 000 draws from 65 536 points collide about eight times.
        Assert.IsGreaterThanOrEqualTo(980, keys.Count);
    }

    [TestMethod]
    public void Keys_past_the_largest_and_smallest_integers_still_exist()
    {
        string afterLargest = BcrdtOrderKey.Between(Largest, null);
        Assert.IsLessThan(0, string.CompareOrdinal(Largest, afterLargest));

        string lowest = BcrdtOrderKey.Between(null, Smallest + "1");
        Assert.IsTrue(BcrdtOrderKey.IsValid(lowest));
        Assert.IsLessThan(0, string.CompareOrdinal(lowest, Smallest + "1"));
        Assert.IsLessThan(0, string.CompareOrdinal(BcrdtOrderKey.Between(null, lowest), lowest));
    }

    [TestMethod]
    public void A_very_long_key_from_a_peer_does_not_overflow_the_stack()
    {
        string before = "a0" + new string('z', 200_000) + "1";
        string after = "a1";

        string key = BcrdtOrderKey.Between(before, after);

        Assert.IsTrue(string.CompareOrdinal(before, key) < 0 && string.CompareOrdinal(key, after) < 0);
    }
}
