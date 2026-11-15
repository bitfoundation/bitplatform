namespace Bit.Bink.Tests;

/// <summary>Fractional order keys: always room between two, always in order.</summary>
[TestClass]
public sealed class OrderKeyTests
{
    [TestMethod]
    public void A_key_between_two_sorts_between_them()
    {
        string middle = BinkOrderKey.Between(null, null);
        string low = BinkOrderKey.Between(null, middle);
        string high = BinkOrderKey.Between(middle, null);
        string inner = BinkOrderKey.Between(low, middle);

        Assert.IsLessThan(0, BinkOrderKey.Compare(low, inner));
        Assert.IsLessThan(0, BinkOrderKey.Compare(inner, middle));
        Assert.IsLessThan(0, BinkOrderKey.Compare(middle, high));
        foreach (string k in new[] { middle, low, high, inner }) Assert.IsTrue(BinkOrderKey.IsValid(k), k);
    }

    [TestMethod]
    public void Thousands_of_random_insertions_stay_strictly_ordered_and_short()
    {
        var random = new Random(7);
        var keys = new List<string>();
        for (int i = 0; i < 5000; i++)
        {
            int at = random.Next(keys.Count + 1);
            string key = BinkOrderKey.Between(at > 0 ? keys[at - 1] : null, at < keys.Count ? keys[at] : null);
            Assert.IsTrue(BinkOrderKey.IsValid(key), key);
            keys.Insert(at, key);
        }
        for (int i = 1; i < keys.Count; i++) Assert.IsLessThan(0, BinkOrderKey.Compare(keys[i - 1], keys[i]));
        Assert.IsLessThan(12, keys.Max(k => k.Length), "Random insertions keep keys short.");
    }

    [TestMethod]
    public void Inserting_again_and_again_at_one_spot_grows_keys_slowly()
    {
        string? top = null;
        string bottom = BinkOrderKey.Between(null, null);
        for (int i = 0; i < 600; i++) top = BinkOrderKey.Between(bottom, top);
        Assert.IsLessThan(150, top!.Length, "About a character every five or six insertions: the halving a fraction allows.");

        string? last = null;
        for (int i = 0; i < 600; i++) last = BinkOrderKey.Between(last, null);
        Assert.IsLessThanOrEqualTo(3, last!.Length, "Appending on top, the drawing case, steps the integer part.");
    }

    [TestMethod]
    public void Spread_keys_are_increasing_valid_and_as_short_as_the_count_allows()
    {
        foreach (int count in new[] { 0, 1, 2, 61, 62, 63, 3843, 3844, 10_000 })
        {
            string[] keys = BinkOrderKey.Spread(count);
            Assert.HasCount(count, keys);
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(BinkOrderKey.IsValid(keys[i]), $"{count}: {keys[i]}");
                if (i > 0) Assert.IsLessThan(0, BinkOrderKey.Compare(keys[i - 1], keys[i]), $"{count} at {i}");
            }
            if (count > 0) Assert.IsLessThanOrEqualTo(count <= 62 ? 2 : count <= 62 + 3844 ? 3 : 4, keys.Max(k => k.Length));
        }
        // There is room before the first and after the last.
        string[] spread = BinkOrderKey.Spread(100);
        Assert.IsLessThan(0, BinkOrderKey.Compare(BinkOrderKey.Between(null, spread[0]), spread[0]));
        Assert.IsGreaterThan(0, BinkOrderKey.Compare(BinkOrderKey.Between(spread[^1], null), spread[^1]));
    }

    [TestMethod]
    public void Malformed_keys_are_refused()
    {
        foreach (string? bad in new[] { "", "a", "a0V0", "a b", "aé", "0a", "A" + new string('0', 26), "a" + new string('V', BinkOrderKey.MaxLength) }) Assert.IsFalse(BinkOrderKey.IsValid(bad), bad);
        Assert.ThrowsExactly<ArgumentException>(() => BinkOrderKey.Between("a1", "a0"));
        Assert.ThrowsExactly<ArgumentException>(() => BinkOrderKey.Between("a0", "a0"));
        Assert.ThrowsExactly<ArgumentException>(() => BinkOrderKey.Between("a00", null));
    }
}
