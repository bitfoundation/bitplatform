namespace Bit.Bcrdt.Testing;

/// <summary>What a randomized run is allowed to do.</summary>
public sealed class BcrdtOperationGeneratorOptions
{
    /// <summary>Whether text edits are generated.</summary>
    public bool Text { get; init; } = true;

    /// <summary>Whether sequence edits are generated.</summary>
    public bool Array { get; init; } = true;

    /// <summary>Whether map edits are generated.</summary>
    public bool Map { get; init; } = true;

    /// <summary>Whether rich-text formatting is generated.</summary>
    public bool Formatting { get; init; } = true;

    /// <summary>Whether nested types are created.</summary>
    public bool NestedTypes { get; init; } = true;

    /// <summary>How many mutations a single generated transaction may contain.</summary>
    public int MaxOperationsPerTransaction { get; init; } = 4;

    /// <summary>How many distinct map keys to draw from. Fewer means more write conflicts.</summary>
    public int KeySpace { get; init; } = 8;
}

/// <summary>
/// Generates random but valid edits, for property-based convergence testing.
/// </summary>
/// <remarks>
/// <para>
/// Hand-written tests check the cases their author thought of, and convergence bugs live in the
/// cases nobody thought of - an insert landing exactly at a block boundary that another replica
/// split a moment earlier, a delete that spans a formatting mark, a map key written concurrently by
/// three peers. Randomized generation explores that space far faster than hand-written examples can.
/// </para>
/// <para>
/// Every generator is driven by an explicit seed, so a failing schedule is reproducible and can be
/// narrowed by hand. A randomized test that cannot be replayed is not a test, only an anecdote.
/// </para>
/// </remarks>
public sealed class BcrdtOperationGenerator(int seed, BcrdtOperationGeneratorOptions? options = null)
{
    private static readonly string[] Words =
    [
        "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog",
        "hello", "world", "collaborative", "editing", "converges", "\u00e9\u00e0\u00fc", "\ud83d\ude00", "\n",
    ];

    private readonly Random _random = new(seed);
    private readonly BcrdtOperationGeneratorOptions _options = options ?? new BcrdtOperationGeneratorOptions();

    /// <summary>The seed this generator was created with. Print it in failure messages.</summary>
    public int Seed => seed;

    /// <summary>Applies one randomly chosen transaction to a document.</summary>
    /// <returns>A human-readable description of what it did, for failure diagnostics.</returns>
    public string ApplyRandomTransaction(BcrdtDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        int count = _random.Next(1, _options.MaxOperationsPerTransaction + 1);
        List<string> performed = [];

        doc.Transact(_ =>
        {
            for (int i = 0; i < count; i++)
            {
                string? description = ApplyRandomOperation(doc);
                if (description is not null) performed.Add(description);
            }
        });

        return performed.Count == 0 ? "(no-op)" : string.Join("; ", performed);
    }

    private string? ApplyRandomOperation(BcrdtDocument doc)
    {
        List<Func<string?>> choices = [];
        if (_options.Text) choices.Add(() => TextOperation(doc));
        if (_options.Array) choices.Add(() => ArrayOperation(doc));
        if (_options.Map) choices.Add(() => MapOperation(doc));

        return choices.Count == 0 ? null : choices[_random.Next(choices.Count)]();
    }

    private string? TextOperation(BcrdtDocument doc)
    {
        BcrdtText text = doc.GetText("body");

        // Bias towards insertion so documents grow rather than hovering near empty, where most of
        // the interesting concurrent cases cannot occur.
        int roll = _random.Next(10);

        if (roll < 5 || text.Count == 0)
        {
            int index = _random.Next(text.Count + 1);
            string word = Words[_random.Next(Words.Length)];
            text.Insert(index, word);
            return $"text.insert({index}, \"{word.Replace("\n", "\\n")}\")";
        }

        if (roll < 8)
        {
            int index = _random.Next(text.Count);
            int length = Math.Min(_random.Next(1, 6), text.Count - index);
            text.Delete(index, length);
            return $"text.delete({index}, {length})";
        }

        if (_options.Formatting && text.Count > 0)
        {
            int index = _random.Next(text.Count);
            int length = Math.Min(_random.Next(1, 8), text.Count - index);
            string attribute = _random.Next(2) == 0 ? "bold" : "italic";
            object? value = _random.Next(3) == 0 ? null : true;
            text.Format(index, length, new Dictionary<string, object?> { [attribute] = value });
            return $"text.format({index}, {length}, {attribute}={value?.ToString() ?? "null"})";
        }

        return null;
    }

    private string? ArrayOperation(BcrdtDocument doc)
    {
        BcrdtArray array = doc.GetArray("items");
        int roll = _random.Next(10);

        if (roll < 6 || array.Count == 0)
        {
            int index = _random.Next(array.Count + 1);
            object? value = RandomValue(doc);
            array.Insert(index, value);
            return $"array.insert({index}, {Format(value)})";
        }

        int deleteIndex = _random.Next(array.Count);
        int deleteLength = Math.Min(_random.Next(1, 4), array.Count - deleteIndex);
        array.Delete(deleteIndex, deleteLength);
        return $"array.delete({deleteIndex}, {deleteLength})";
    }

    private string? MapOperation(BcrdtDocument doc)
    {
        BcrdtMap map = doc.GetMap("meta");
        string key = "k" + _random.Next(_options.KeySpace);

        if (_random.Next(5) == 0 && map.ContainsKey(key))
        {
            map.Delete(key);
            return $"map.delete({key})";
        }

        object? value = RandomValue(doc);
        map.Set(key, value);
        return $"map.set({key}, {Format(value)})";
    }

    private object? RandomValue(BcrdtDocument doc)
    {
        int roll = _random.Next(_options.NestedTypes ? 8 : 6);
        return roll switch
        {
            0 => _random.Next(1000),
            1 => Words[_random.Next(Words.Length)],
            2 => _random.NextDouble(),
            3 => _random.Next(2) == 0,
            4 => null,
            5 => new List<object?> { _random.Next(10), Words[_random.Next(Words.Length)] },
            6 => new BcrdtMap(),
            _ => new BcrdtArray(),
        };
    }

    private static string Format(object? value) => value switch
    {
        null => "null",
        BcrdtAbstractType type => $"<{type.Kind}>",
        string text => $"\"{text.Replace("\n", "\\n")}\"",
        _ => value.ToString() ?? "?",
    };
}
