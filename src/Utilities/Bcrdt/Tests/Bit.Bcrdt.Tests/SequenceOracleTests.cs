namespace Bit.Bcrdt.Tests;

/// <summary>
/// Checks the sequence types against a plain <see cref="List{T}"/> doing the same operations.
/// </summary>
/// <remarks>
/// Concurrency is not involved here. These tests answer a prior question: does a single replica,
/// editing alone, behave exactly like an ordinary list? Index arithmetic, block splitting and the
/// position cache all have to be right before convergence between replicas can mean anything, and a
/// failure here is far easier to diagnose than the same bug surfacing as a divergence.
/// </remarks>
[TestClass]
public class SequenceOracleTests
{
    public static IEnumerable<object[]> Seeds
    {
        get
        {
            for (int seed = 1; seed <= 20; seed++) yield return [seed];
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Array_matches_a_plain_list_under_random_editing(int seed)
    {
        var random = new Random(seed);
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtArray array = doc.GetArray("items");
        List<object?> oracle = [];

        for (int step = 0; step < 400; step++)
        {
            if (oracle.Count == 0 || random.Next(10) < 6)
            {
                int index = random.Next(oracle.Count + 1);
                int count = random.Next(1, 4);
                object?[] values = [.. Enumerable.Range(0, count).Select(i => (object?)(long)random.Next(1000))];

                array.Insert(index, values);
                oracle.InsertRange(index, values);
            }
            else
            {
                int index = random.Next(oracle.Count);
                int length = Math.Min(random.Next(1, 4), oracle.Count - index);

                array.Delete(index, length);
                oracle.RemoveRange(index, length);
            }

            Assert.AreEqual(oracle.Count, array.Count);
            Assert.AreSequenceEqual(oracle, array.ToArray());
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Text_matches_a_plain_string_builder_under_random_editing(int seed)
    {
        var random = new Random(seed);
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        var oracle = new System.Text.StringBuilder();

        string[] fragments = ["a", "hello", "xyz", "\n", "much longer fragment of text"];

        for (int step = 0; step < 400; step++)
        {
            if (oracle.Length == 0 || random.Next(10) < 6)
            {
                int index = random.Next(oracle.Length + 1);
                string fragment = fragments[random.Next(fragments.Length)];

                text.Insert(index, fragment);
                oracle.Insert(index, fragment);
            }
            else
            {
                int index = random.Next(oracle.Length);
                int length = Math.Min(random.Next(1, 6), oracle.Length - index);

                text.Delete(index, length);
                oracle.Remove(index, length);
            }

            Assert.AreEqual(oracle.Length, text.Count);
            Assert.AreEqual(oracle.ToString(), text.ToString());
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Array_indexing_stays_correct_when_nested_types_are_mixed_in(int seed)
    {
        var random = new Random(seed);
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtArray array = doc.GetArray("items");
        int expected = 0;

        for (int step = 0; step < 200; step++)
        {
            if (expected == 0 || random.Next(10) < 6)
            {
                int index = random.Next(expected + 1);
                object? value = random.Next(3) switch
                {
                    0 => new BcrdtMap(),
                    1 => new BcrdtArray(),
                    _ => (long)random.Next(100),
                };

                array.Insert(index, value);
                expected++;
            }
            else
            {
                int index = random.Next(expected);
                int length = Math.Min(random.Next(1, 3), expected - index);

                array.Delete(index, length);
                expected -= length;
            }

            Assert.AreEqual(expected, array.Count);
            Assert.AreEqual(expected, array.ToArray().Count);
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Formatting_marks_never_shift_a_character_index(int seed)
    {
        var random = new Random(seed);
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        var oracle = new System.Text.StringBuilder();

        for (int step = 0; step < 200; step++)
        {
            int roll = random.Next(10);

            if (oracle.Length == 0 || roll < 5)
            {
                int index = random.Next(oracle.Length + 1);
                string fragment = new((char)('a' + random.Next(26)), random.Next(1, 5));

                text.Insert(index, fragment);
                oracle.Insert(index, fragment);
            }
            else if (roll < 8)
            {
                int index = random.Next(oracle.Length);
                int length = Math.Min(random.Next(1, 4), oracle.Length - index);

                text.Delete(index, length);
                oracle.Remove(index, length);
            }
            else
            {
                // Formatting must be invisible to the plain-text view and to every index.
                int index = random.Next(oracle.Length);
                int length = Math.Min(random.Next(1, 6), oracle.Length - index);
                text.Format(index, length, new Dictionary<string, object?>
                {
                    [random.Next(2) == 0 ? "bold" : "italic"] = random.Next(4) == 0 ? null : true,
                });
            }

            Assert.AreEqual(oracle.ToString(), text.ToString());
            Assert.AreEqual(oracle.Length, text.Count);
        }
    }
}
