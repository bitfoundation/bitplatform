using Bit.Blex.Query;

namespace Bit.Blex.Tests.Query;

public enum Filter { All, Done }

public sealed record Page(int Number, int Size);

[TestClass]
public class BlexQueryKeyTests
{
    [TestMethod]
    public void Keys_with_equal_parts_are_equal_and_hash_alike()
    {
        BlexQueryKey a = ["todos", 42, Filter.Done, new Page(1, 20)];
        BlexQueryKey b = ["todos", 42, Filter.Done, new Page(1, 20)];

        Assert.AreEqual(a, b);
        Assert.IsTrue(a == b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        Assert.AreNotEqual(a, new BlexQueryKey("todos", 43, Filter.Done, new Page(1, 20)));
        Assert.AreNotEqual(a, new BlexQueryKey("todos", 42));
    }

    [TestMethod]
    public void Order_matters()
        => Assert.AreNotEqual(new BlexQueryKey("a", "b"), new BlexQueryKey("b", "a"));

    [TestMethod]
    public void A_string_is_a_one_part_key()
    {
        BlexQueryKey key = "todos";
        Assert.AreEqual(new BlexQueryKey("todos"), key);
        Assert.AreEqual(1, key.Count);
    }

    [TestMethod]
    public void A_key_starts_with_its_leading_parts()
    {
        BlexQueryKey one = ["todos", 42];

        Assert.IsTrue(one.StartsWith(["todos"]));
        Assert.IsTrue(one.StartsWith(["todos", 42]));
        Assert.IsFalse(one.StartsWith(["todos", 43]));
        Assert.IsFalse(one.StartsWith(["todos", 42, "comments"]));
        Assert.IsFalse(one.StartsWith(["users"]));
    }

    [TestMethod]
    public void It_prints_as_stable_json_like_text()
    {
        BlexQueryKey key = ["todos", 42, 1.5, true, null, Filter.Done, "say \"hi\""];
        Assert.AreEqual("[\"todos\",42,1.5,true,null,\"Done\",\"say \\\"hi\\\"\"]", key.ToString());
    }

    [TestMethod]
    public void An_empty_key_is_rejected()
        => Assert.ThrowsExactly<ArgumentException>(() => new BlexQueryKey());

    [TestMethod]
    public void It_does_not_alias_the_array_it_was_given()
    {
        var parts = new object?[] { "todos", 1 };
        var key = new BlexQueryKey(parts);
        parts[1] = 2;

        Assert.AreEqual(new BlexQueryKey("todos", 1), key);
    }
}
