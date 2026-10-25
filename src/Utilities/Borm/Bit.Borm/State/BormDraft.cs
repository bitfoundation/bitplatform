using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace Bit.Borm;

/// <summary>
/// Saves a form's unfinished answers as a small JSON document and puts them back — the "you were
/// halfway through this, carry on?" a long form owes its user when the tab closes, the session expires
/// or the phone rings. Where the document is kept (browser storage, the server, a database) is the
/// application's choice; this only writes and reads it.
/// </summary>
/// <remarks>
/// <para>
/// A draft holds every value the schema binds to, one per path (<c>Lines[2].Quantity</c>), with the
/// number of rows each repeater had and the wizard step the user was on. It never holds a file — a
/// browser file cannot outlive the page that chose it — nor a sensitive value (a password, a field
/// marked <see cref="BormFieldDefinition.Sensitive"/>) unless <see cref="BormDraftOptions.IncludeSensitive"/>
/// says so; a computed value is left out and computed again.
/// </para>
/// <para>
/// A restored draft is the user's unsaved work, so it lands as changes on top of what the form opened
/// with: <see cref="BormState.IsFormDirty"/> reports it, and <see cref="BormState.Reset()"/> takes it
/// away again. Values the current schema has no field for, or can no longer hold, are reported in
/// <see cref="BormDraftRestoreResult.Skipped"/> rather than silently dropped — and a draft saved
/// against another <see cref="BormDefinition.Version"/> can be brought up to date first with
/// <see cref="BormDraftOptions.Migrate"/>.
/// </para>
/// <para>
/// A draft is plain JSON holding personal data. Keep it where the user's other data is kept, clear it
/// once the form has been submitted, and do not write it to storage another user of the same machine
/// can read.
/// </para>
/// </remarks>
public static class BormDraft
{
    private const string Format = "borm-draft";
    private const int FormatVersion = 1;

    /// <summary>
    /// The most rows a draft may restore, all its repeaters together, whatever it says they had — the same
    /// bound the JSON Schema importer puts on a whole form. A draft comes back from storage the application
    /// does not fully control, and "a billion rows" is a few bytes of it; so is "ten thousand rows, each with
    /// ten thousand rows of its own", which a bound on each list alone let through.
    /// </summary>
    public const int MaxRows = 10_000;

    /// <summary>Writes the form's answers as a draft document.</summary>
    public static string Serialize(BormState state, BormDraftOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        options ??= new BormDraftOptions();

        HashSet<string>? visible = null;
        if (options.ExcludeHidden)
            visible = new(state.Snapshot(new BormSnapshotOptions { ExcludeHidden = true }).Keys, StringComparer.OrdinalIgnoreCase);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("format", Format);
            writer.WriteNumber("formatVersion", FormatVersion);
            if (state.Definition.Version is { } version) writer.WriteString("version", version);
            writer.WriteString("savedAt", (options.TimeProvider ?? TimeProvider.System).GetUtcNow());
            if (state.Definition.HasSteps) writer.WriteNumber("step", state.CurrentStepIndex);

            var rows = new List<(string Path, int Count)>();
            writer.WritePropertyName("values");
            writer.WriteStartObject();
            foreach (var (field, path) in state.FieldPaths())
            {
                if (field.Type == BormFieldType.Object || field.IsPresentational) continue;
                if (field.Type == BormFieldType.Array)
                {
                    rows.Add((path, state.ArrayCount(path)));
                    continue;
                }
                if (field.Computed is not null || field.Type == BormFieldType.File) continue;
                if (field.IsSensitive && !options.IncludeSensitive) continue;
                if (visible is not null && !visible.Contains(path)) continue;

                var value = state.GetValue(path);
                if (!IsStorable(value)) continue;
                writer.WritePropertyName(path);
                // A timestamp keeps its offset: its input shows only the clock time, but the instant is
                // what the draft has to give back.
                BormConditionJson.WriteValue(writer, value, value is DateTimeOffset ? null : field.Type);
            }
            writer.WriteEndObject();

            writer.WritePropertyName("rows");
            writer.WriteStartObject();
            foreach (var (path, count) in rows) writer.WriteNumber(path, count);
            writer.WriteEndObject();

            writer.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Puts a draft's answers back into the form. Returns what happened; a document that is not a draft
    /// changes nothing and says so.
    /// </summary>
    public static BormDraftRestoreResult Restore(BormState state, string json, BormDraftOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        options ??= new BormDraftOptions();

        BormDraftContent content;
        try
        {
            content = Read(json);
        }
        catch (JsonException ex)
        {
            // Said in the library's own words: a trimmed WebAssembly app keeps the runtime's resource keys rather than
            // its messages, and "ExpectedStartOfPropertyNotFound, n LineNumber: 0" is what the page was shown.
            return BormDraftRestoreResult.NotADraft(ex.LineNumber is { } line && ex.BytePositionInLine is { } position
                ? $"The draft is not valid JSON (line {line + 1}, position {position + 1})."
                : "The draft is not valid JSON.");
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException or ArgumentException)
        {
            return BormDraftRestoreResult.NotADraft(ex.Message);
        }

        var migrated = false;
        if (options.Migrate is { } migrate && !string.Equals(content.Version, state.Definition.Version, StringComparison.Ordinal))
        {
            migrate(new BormDraftMigration(content, state.Definition.Version));
            migrated = true;
        }

        var skipped = new List<string>();
        state.Restoring(() =>
        {
            var budget = MaxRows;
            // Outer lists first, so a row inside a row exists by the time its own rows are counted - and a list
            // inside a row that does not exist, the outer list having been cut short or never having had it, is
            // skipped like a value there would be, rather than filled in a list that belongs to nothing.
            foreach (var (path, count) in content.Rows.OrderBy(r => r.Key.Count(c => c is '[' or '.')).ThenBy(r => r.Key, StringComparer.Ordinal))
            {
                if (!BormPath.IsValid(path) || state.FindField(path) is not { Type: BormFieldType.Array } array || !state.IsLiveForRestore(path))
                {
                    skipped.Add(path);
                    continue;
                }
                var target = Math.Clamp(count, 0, Math.Min(array.MaxItems ?? MaxRows, budget));
                if (target < count) skipped.Add(path);
                budget = Math.Max(0, budget - state.SetRowCount(array, path, target));
            }

            foreach (var (path, value) in content.Values)
            {
                var field = BormPath.IsValid(path) ? state.FindField(path) : null;
                if (field is null || field.IsContainer || field.IsPresentational || field.Computed is not null
                    || field.Type == BormFieldType.File || !state.IsLiveForRestore(path)
                    || !state.TryRestoreValue(path, value))
                    skipped.Add(path);
            }

            if (content.Step is { } step && state.Definition.HasSteps) state.GoToStep(step);
        });

        return new BormDraftRestoreResult(true, content.Version, content.SavedAt, migrated, skipped, null);
    }

    private static BormDraftContent Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String
            || format.GetString() != Format)
            throw new FormatException("The document is not a Borm draft.");
        if (root.TryGetProperty("formatVersion", out var fv) && fv.ValueKind == JsonValueKind.Number && fv.GetInt32() > FormatVersion)
            throw new FormatException("The draft was written by a newer version of Borm.");

        var content = new BormDraftContent
        {
            Version = root.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null,
            SavedAt = root.TryGetProperty("savedAt", out var s) && s.ValueKind == JsonValueKind.String
                      && DateTimeOffset.TryParse(s.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)
                ? at : null,
            Step = root.TryGetProperty("step", out var st) && st.ValueKind == JsonValueKind.Number && st.TryGetInt32(out var step) ? step : null
        };

        if (root.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Object)
            foreach (var entry in values.EnumerateObject())
                content.Values[entry.Name] = BormConditionJson.ReadValue(entry.Value);

        if (root.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Object)
            foreach (var entry in rows.EnumerateObject())
                if (entry.Value.ValueKind == JsonValueKind.Number && entry.Value.TryGetInt32(out var count) && count >= 0)
                    content.Rows[entry.Name] = count;

        return content;
    }

    /// <summary>Whether a value has a JSON form a draft can keep: a browser file, a stream or a delegate does not.</summary>
    private static bool IsStorable(object? value) => value switch
    {
        null or string or bool or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan or Guid or Enum => true,
        _ when BormNumber.IsNumber(value) => BormConditionJson.IsWritable(value),
        Microsoft.AspNetCore.Components.Forms.IBrowserFile or Stream or Delegate => false,
        IEnumerable items => items.Cast<object?>().All(IsStorable),
        _ => false
    };
}

/// <summary>How <see cref="BormDraft"/> writes and reads a draft.</summary>
public sealed class BormDraftOptions
{
    /// <summary>Keep sensitive values (passwords, fields marked <c>Sensitive()</c>) in the draft. Off by default.</summary>
    public bool IncludeSensitive { get; init; }

    /// <summary>Leave out the values of fields the form is hiding when the draft is written.</summary>
    public bool ExcludeHidden { get; init; }

    /// <summary>
    /// Brings a draft saved against another version of the form up to date before it is restored:
    /// called when the draft's version differs from <see cref="BormDefinition.Version"/>, with the
    /// draft's values and row counts to rename, convert or drop.
    /// </summary>
    public Action<BormDraftMigration>? Migrate { get; init; }

    /// <summary>The clock the draft's <c>savedAt</c> is read from. The system clock when null.</summary>
    public TimeProvider? TimeProvider { get; init; }
}

/// <summary>A draft being brought up to date. See <see cref="BormDraftOptions.Migrate"/>.</summary>
public sealed class BormDraftMigration
{
    private readonly BormDraftContent _content;

    internal BormDraftMigration(BormDraftContent content, string? toVersion)
    {
        _content = content;
        ToVersion = toVersion;
    }

    /// <summary>The version of the form the draft was saved against, or null when it did not say.</summary>
    public string? FromVersion => _content.Version;

    /// <summary>The version of the form it is being restored into.</summary>
    public string? ToVersion { get; }

    /// <summary>Every saved value by path — edit it in place.</summary>
    public IDictionary<string, object?> Values => _content.Values;

    /// <summary>How many rows each repeater had, by path — edit it in place.</summary>
    public IDictionary<string, int> Rows => _content.Rows;

    /// <summary>
    /// Moves a field that was renamed or moved — its value, and for a container everything saved
    /// beneath it, rows included: <c>Rename("Phone", "Contact.Phone")</c>.
    /// </summary>
    public BormDraftMigration Rename(string from, string to)
    {
        ArgumentException.ThrowIfNullOrEmpty(from);
        ArgumentException.ThrowIfNullOrEmpty(to);
        Move(_content.Values, from, to);
        Move(_content.Rows, from, to);
        return this;

        static void Move<T>(IDictionary<string, T> map, string from, string to)
        {
            foreach (var key in map.Keys.Where(k => BormPath.IsAtOrUnder(k, from)).ToList())
            {
                var value = map[key];
                map.Remove(key);
                map[to + key[from.Length..]] = value;
            }
        }
    }

    /// <summary>Drops a field that no longer exists, and everything saved beneath it.</summary>
    public BormDraftMigration Remove(string path)
    {
        foreach (var key in _content.Values.Keys.Where(k => BormPath.IsAtOrUnder(k, path)).ToList()) _content.Values.Remove(key);
        foreach (var key in _content.Rows.Keys.Where(k => BormPath.IsAtOrUnder(k, path)).ToList()) _content.Rows.Remove(key);
        return this;
    }
}

/// <summary>What a draft held, as read back.</summary>
internal sealed class BormDraftContent
{
    public string? Version { get; set; }
    public DateTimeOffset? SavedAt { get; set; }
    public int? Step { get; set; }
    public Dictionary<string, object?> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Rows { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>What <see cref="BormDraft.Restore"/> did.</summary>
/// <param name="Restored">Whether the document was a draft and its answers were put back.</param>
/// <param name="Version">The form version the draft was saved against.</param>
/// <param name="SavedAt">When it was saved.</param>
/// <param name="Migrated">Whether <see cref="BormDraftOptions.Migrate"/> was asked to bring it up to date.</param>
/// <param name="Skipped">Saved paths the form has no field for, or whose field can no longer hold the saved value.</param>
/// <param name="Error">Why the document was not read as a draft, when it was not.</param>
public sealed record BormDraftRestoreResult(
    bool Restored,
    string? Version,
    DateTimeOffset? SavedAt,
    bool Migrated,
    IReadOnlyList<string> Skipped,
    string? Error)
{
    internal static BormDraftRestoreResult NotADraft(string error) => new(false, null, null, false, [], error);
}
