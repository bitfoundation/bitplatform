namespace Bit.Bcrdt.Tests;

/// <summary>
/// Checks that a replica's own view stays internally consistent while remote updates arrive.
/// </summary>
/// <remarks>
/// Separate from the convergence suite on purpose. These assert the narrower property that a
/// replica's reported length always matches its actual contents, which is what breaks first when
/// remote integration disturbs cached index positions - and which produces a far clearer failure
/// than the divergence it would eventually cause.
/// </remarks>
[TestClass]
public class ReplicaConsistencyTests
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void Array_length_stays_consistent_across_replicas(int seed)
    {
        var random = new Random(seed);
        using var a = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var b = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });

        for (int round = 0; round < 200; round++)
        {
            foreach (BcrdtDocument doc in new[] { a, b })
            {
                BcrdtArray array = doc.GetArray("items");
                if (array.Count == 0 || random.Next(10) < 6)
                {
                    array.Insert(random.Next(array.Count + 1), (long)random.Next(100));
                }
                else
                {
                    int index = random.Next(array.Count);
                    array.Delete(index, Math.Min(random.Next(1, 4), array.Count - index));
                }
            }

            a.ApplyUpdate(b.EncodeStateAsUpdate(a.GetStateVector()), "sync");
            b.ApplyUpdate(a.EncodeStateAsUpdate(b.GetStateVector()), "sync");

            Assert.AreEqual(a.GetArray("items").ToArray().Count, a.GetArray("items").Count);
            Assert.AreEqual(b.GetArray("items").ToArray().Count, b.GetArray("items").Count);
            Assert.AreSequenceEqual(a.GetArray("items").ToArray(), b.GetArray("items").ToArray());
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void Text_length_stays_consistent_across_replicas(int seed)
    {
        var random = new Random(seed);
        using var a = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var b = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });

        for (int round = 0; round < 200; round++)
        {
            foreach (BcrdtDocument doc in new[] { a, b })
            {
                BcrdtText text = doc.GetText("body");
                if (text.Count == 0 || random.Next(10) < 6)
                {
                    text.Insert(random.Next(text.Count + 1), "abc"[random.Next(3)].ToString());
                }
                else
                {
                    int index = random.Next(text.Count);
                    text.Delete(index, Math.Min(random.Next(1, 4), text.Count - index));
                }
            }

            a.ApplyUpdate(b.EncodeStateAsUpdate(a.GetStateVector()), "sync");
            b.ApplyUpdate(a.EncodeStateAsUpdate(b.GetStateVector()), "sync");

            Assert.AreEqual(a.GetText("body").ToString().Length, a.GetText("body").Count);
            Assert.AreEqual(b.GetText("body").ToString().Length, b.GetText("body").Count);
            Assert.AreEqual(a.GetText("body").ToString(), b.GetText("body").ToString());
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public void Text_with_formatting_stays_consistent_across_replicas(int seed)
    {
        var random = new Random(seed);
        using var a = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var b = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });

        for (int round = 0; round < 150; round++)
        {
            foreach (BcrdtDocument doc in new[] { a, b })
            {
                BcrdtText text = doc.GetText("body");
                int roll = random.Next(10);
                if (text.Count == 0 || roll < 5)
                {
                    text.Insert(random.Next(text.Count + 1), "abc"[random.Next(3)].ToString());
                }
                else if (roll < 8)
                {
                    int index = random.Next(text.Count);
                    text.Delete(index, Math.Min(random.Next(1, 4), text.Count - index));
                }
                else
                {
                    int index = random.Next(text.Count);
                    text.Format(index, Math.Min(random.Next(1, 4), text.Count - index),
                        new Dictionary<string, object?> { ["bold"] = random.Next(3) == 0 ? null : true });
                }
            }

            a.ApplyUpdate(b.EncodeStateAsUpdate(a.GetStateVector()), "sync");
            b.ApplyUpdate(a.EncodeStateAsUpdate(b.GetStateVector()), "sync");

            Assert.AreEqual(a.GetText("body").ToString().Length, a.GetText("body").Count);
            Assert.AreEqual(b.GetText("body").ToString().Length, b.GetText("body").Count);
            Assert.AreEqual(a.GetText("body").ToString(), b.GetText("body").ToString());
        }
    }
}
