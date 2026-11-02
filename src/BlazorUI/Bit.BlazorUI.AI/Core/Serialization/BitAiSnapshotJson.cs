using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// The metadata the conversation snapshots are serialized with: the package's source-generated context
/// (<see cref="BitAiJsonContext"/>), with the snapshot records (and the attachments they carry) read the way the
/// reflection-based serializer read them.
/// </summary>
/// <remarks>
/// <para>
/// The source generator creates a record with init-only properties in one object initializer that assigns every one of
/// them, so a property the JSON leaves out is assigned its default rather than keeping its initializer: a snapshot without
/// <c>schemaVersion</c> would read as version 0, one without <c>messages</c> as a null list, a message without
/// <c>status</c> as a draft. The reflection-based serializer created the record first and set only the properties the
/// JSON had, which is what a persisted format needs to read what an older (or a hand-written) producer left out. The JSON
/// written is the same either way.
/// </para>
/// <para>
/// So for each snapshot record that initializes a property to something other than its default, the contract here
/// creates the record with its initializers and sets each property the JSON has through its init accessor, which
/// <see cref="UnsafeAccessorAttribute"/> reaches without reflection, so it is safe to trim and to compile ahead of time.
/// A record whose properties all start at their default (<see cref="BitAiPartSnapshot"/>,
/// <see cref="BitAiToolOutcomeSnapshot"/>) reads the same either way and keeps the generated contract.
/// </para>
/// <para>
/// A <see cref="BitAiAttachment"/> is otherwise created through its constructor, whose arguments only the JSON has, so
/// it is created first through a private constructor kept for this, and its identifier and name are then checked the
/// way the public constructor checks them: an attachment without a <c>status</c> keeps
/// <see cref="BitAiAttachmentStatus.Ready"/>, and one without an <c>attachmentId</c> still fails with the same exception.
/// </para>
/// </remarks>
internal static class BitAiSnapshotJson
{
    /// <summary>The resolver of the settings the snapshots are serialized with.</summary>
    internal static IJsonTypeInfoResolver Resolver { get; } = BitAiJsonContext.Default.WithAddedModifier(ReadLikeReflection);

    private static readonly Member[] ConversationMembers =
    [
        new(nameof(BitAiConversationSnapshot.SchemaVersion), static (o, v) => SetSchemaVersion((BitAiConversationSnapshot)o, (int)v!)),
        new(nameof(BitAiConversationSnapshot.ConversationId), static (o, v) => SetConversationId((BitAiConversationSnapshot)o, (string)v!)),
        new(nameof(BitAiConversationSnapshot.Title), static (o, v) => SetTitle((BitAiConversationSnapshot)o, (string?)v)),
        new(nameof(BitAiConversationSnapshot.CreatedAt), static (o, v) => SetCreatedAt((BitAiConversationSnapshot)o, (DateTimeOffset)v!)),
        new(nameof(BitAiConversationSnapshot.UpdatedAt), static (o, v) => SetUpdatedAt((BitAiConversationSnapshot)o, (DateTimeOffset)v!)),
        new(nameof(BitAiConversationSnapshot.Messages), static (o, v) => SetMessages((BitAiConversationSnapshot)o, (IReadOnlyList<BitAiMessageSnapshot>)v!)),
        new(nameof(BitAiConversationSnapshot.Artifacts), static (o, v) => SetArtifacts((BitAiConversationSnapshot)o, (IReadOnlyList<BitAiArtifactSnapshot>)v!)),
        new(nameof(BitAiConversationSnapshot.Runs), static (o, v) => SetRuns((BitAiConversationSnapshot)o, (IReadOnlyList<BitAiAgentRunSnapshot>)v!)),
        new(nameof(BitAiConversationSnapshot.Extensions), static (o, v) => SetExtensions((BitAiConversationSnapshot)o, (IReadOnlyDictionary<string, JsonElement>?)v)),
    ];

    private static readonly Member[] MessageMembers =
    [
        new(nameof(BitAiMessageSnapshot.MessageId), static (o, v) => SetMessageId((BitAiMessageSnapshot)o, (string)v!)),
        new(nameof(BitAiMessageSnapshot.Role), static (o, v) => SetRole((BitAiMessageSnapshot)o, (BitAiRole)v!)),
        new(nameof(BitAiMessageSnapshot.CreatedAt), static (o, v) => SetCreatedAt((BitAiMessageSnapshot)o, (DateTimeOffset)v!)),
        new(nameof(BitAiMessageSnapshot.Status), static (o, v) => SetStatus((BitAiMessageSnapshot)o, (BitAiMessageStatus)v!)),
        new(nameof(BitAiMessageSnapshot.Metadata), static (o, v) => SetMetadata((BitAiMessageSnapshot)o, (BitAiGenerationMetadata?)v)),
        new(nameof(BitAiMessageSnapshot.Error), static (o, v) => SetError((BitAiMessageSnapshot)o, (BitAiError?)v)),
        new(nameof(BitAiMessageSnapshot.Feedback), static (o, v) => SetFeedback((BitAiMessageSnapshot)o, (BitAiFeedbackKind)v!)),
        new(nameof(BitAiMessageSnapshot.FeedbackComment), static (o, v) => SetFeedbackComment((BitAiMessageSnapshot)o, (string?)v)),
        new(nameof(BitAiMessageSnapshot.RunId), static (o, v) => SetRunId((BitAiMessageSnapshot)o, (string?)v)),
        new(nameof(BitAiMessageSnapshot.Parts), static (o, v) => SetParts((BitAiMessageSnapshot)o, (IReadOnlyList<BitAiPartSnapshot>)v!)),
        new(nameof(BitAiMessageSnapshot.Attachments), static (o, v) => SetAttachments((BitAiMessageSnapshot)o, (IReadOnlyList<BitAiAttachment>)v!)),
        new(nameof(BitAiMessageSnapshot.Citations), static (o, v) => SetCitations((BitAiMessageSnapshot)o, (IReadOnlyList<BitAiCitation>)v!)),
        new(nameof(BitAiMessageSnapshot.Extensions), static (o, v) => SetExtensions((BitAiMessageSnapshot)o, (IReadOnlyDictionary<string, JsonElement>?)v)),
    ];

    private static readonly Member[] ArtifactMembers =
    [
        new(nameof(BitAiArtifactSnapshot.ArtifactId), static (o, v) => SetArtifactId((BitAiArtifactSnapshot)o, (string)v!)),
        new(nameof(BitAiArtifactSnapshot.Kind), static (o, v) => SetKind((BitAiArtifactSnapshot)o, (BitAiArtifactKind)v!)),
        new(nameof(BitAiArtifactSnapshot.Title), static (o, v) => SetTitle((BitAiArtifactSnapshot)o, (string)v!)),
        new(nameof(BitAiArtifactSnapshot.Content), static (o, v) => SetContent((BitAiArtifactSnapshot)o, (string?)v)),
        new(nameof(BitAiArtifactSnapshot.Language), static (o, v) => SetLanguage((BitAiArtifactSnapshot)o, (string?)v)),
        new(nameof(BitAiArtifactSnapshot.Url), static (o, v) => SetUrl((BitAiArtifactSnapshot)o, (string?)v)),
        new(nameof(BitAiArtifactSnapshot.Version), static (o, v) => SetVersion((BitAiArtifactSnapshot)o, (int)v!)),
        new(nameof(BitAiArtifactSnapshot.IsStreaming), static (o, v) => SetIsStreaming((BitAiArtifactSnapshot)o, (bool)v!)),
        new(nameof(BitAiArtifactSnapshot.CreatedAt), static (o, v) => SetCreatedAt((BitAiArtifactSnapshot)o, (DateTimeOffset)v!)),
        new(nameof(BitAiArtifactSnapshot.UpdatedAt), static (o, v) => SetUpdatedAt((BitAiArtifactSnapshot)o, (DateTimeOffset)v!)),
        new(nameof(BitAiArtifactSnapshot.Versions), static (o, v) => SetVersions((BitAiArtifactSnapshot)o, (IReadOnlyList<BitAiArtifactVersion>)v!)),
        new(nameof(BitAiArtifactSnapshot.Metadata), static (o, v) => SetMetadata((BitAiArtifactSnapshot)o, (IReadOnlyDictionary<string, JsonElement>?)v)),
    ];

    private static readonly Member[] RunMembers =
    [
        new(nameof(BitAiAgentRunSnapshot.RunId), static (o, v) => SetRunId((BitAiAgentRunSnapshot)o, (string)v!)),
        new(nameof(BitAiAgentRunSnapshot.AgentName), static (o, v) => SetAgentName((BitAiAgentRunSnapshot)o, (string?)v)),
        new(nameof(BitAiAgentRunSnapshot.Goal), static (o, v) => SetGoal((BitAiAgentRunSnapshot)o, (string?)v)),
        new(nameof(BitAiAgentRunSnapshot.Status), static (o, v) => SetStatus((BitAiAgentRunSnapshot)o, (BitAiAgentRunStatus)v!)),
        new(nameof(BitAiAgentRunSnapshot.PendingPrompt), static (o, v) => SetPendingPrompt((BitAiAgentRunSnapshot)o, (string?)v)),
        new(nameof(BitAiAgentRunSnapshot.PendingSuggestions), static (o, v) => SetPendingSuggestions((BitAiAgentRunSnapshot)o, (IReadOnlyList<string>?)v)),
        new(nameof(BitAiAgentRunSnapshot.StartedAt), static (o, v) => SetStartedAt((BitAiAgentRunSnapshot)o, (DateTimeOffset)v!)),
        new(nameof(BitAiAgentRunSnapshot.CompletedAt), static (o, v) => SetCompletedAt((BitAiAgentRunSnapshot)o, (DateTimeOffset?)v)),
        new(nameof(BitAiAgentRunSnapshot.Usage), static (o, v) => SetUsage((BitAiAgentRunSnapshot)o, (BitAiTokenUsage)v!)),
        new(nameof(BitAiAgentRunSnapshot.Error), static (o, v) => SetError((BitAiAgentRunSnapshot)o, (BitAiError?)v)),
        new(nameof(BitAiAgentRunSnapshot.Steps), static (o, v) => SetSteps((BitAiAgentRunSnapshot)o, (IReadOnlyList<BitAiAgentStepSnapshot>)v!)),
    ];

    private static readonly Member[] StepMembers =
    [
        new(nameof(BitAiAgentStepSnapshot.StepId), static (o, v) => SetStepId((BitAiAgentStepSnapshot)o, (string)v!)),
        new(nameof(BitAiAgentStepSnapshot.Title), static (o, v) => SetTitle((BitAiAgentStepSnapshot)o, (string)v!)),
        new(nameof(BitAiAgentStepSnapshot.Detail), static (o, v) => SetDetail((BitAiAgentStepSnapshot)o, (string?)v)),
        new(nameof(BitAiAgentStepSnapshot.Status), static (o, v) => SetStatus((BitAiAgentStepSnapshot)o, (BitAiAgentStepStatus)v!)),
        new(nameof(BitAiAgentStepSnapshot.Summary), static (o, v) => SetSummary((BitAiAgentStepSnapshot)o, (string?)v)),
        new(nameof(BitAiAgentStepSnapshot.Error), static (o, v) => SetError((BitAiAgentStepSnapshot)o, (BitAiError?)v)),
        new(nameof(BitAiAgentStepSnapshot.StartedAt), static (o, v) => SetStartedAt((BitAiAgentStepSnapshot)o, (DateTimeOffset?)v)),
        new(nameof(BitAiAgentStepSnapshot.CompletedAt), static (o, v) => SetCompletedAt((BitAiAgentStepSnapshot)o, (DateTimeOffset?)v)),
        new(nameof(BitAiAgentStepSnapshot.ToolCallIds), static (o, v) => SetToolCallIds((BitAiAgentStepSnapshot)o, (IReadOnlyList<string>)v!)),
    ];

    private static readonly Member[] AttachmentMembers =
    [
        new(nameof(BitAiAttachment.AttachmentId), static (o, v) => SetAttachmentId((BitAiAttachment)o, (string)v!)),
        new(nameof(BitAiAttachment.Name), static (o, v) => SetName((BitAiAttachment)o, (string)v!)),
        new(nameof(BitAiAttachment.ContentType), static (o, v) => SetContentType((BitAiAttachment)o, (string?)v)),
        new(nameof(BitAiAttachment.Size), static (o, v) => SetSize((BitAiAttachment)o, (long?)v)),
        new(nameof(BitAiAttachment.Kind), static (o, v) => SetKind((BitAiAttachment)o, (BitAiAttachmentKind)v!)),
        new(nameof(BitAiAttachment.Status), static (o, v) => SetStatus((BitAiAttachment)o, (BitAiAttachmentStatus)v!)),
        new(nameof(BitAiAttachment.Progress), static (o, v) => SetProgress((BitAiAttachment)o, (double?)v)),
        new(nameof(BitAiAttachment.Error), static (o, v) => SetError((BitAiAttachment)o, (string?)v)),
        new(nameof(BitAiAttachment.Url), static (o, v) => SetUrl((BitAiAttachment)o, (string?)v)),
        new(nameof(BitAiAttachment.ThumbnailUrl), static (o, v) => SetThumbnailUrl((BitAiAttachment)o, (string?)v)),
        new(nameof(BitAiAttachment.Data), static (o, v) => SetData((BitAiAttachment)o, (ReadOnlyMemory<byte>?)v)),
        new(nameof(BitAiAttachment.Metadata), static (o, v) => SetMetadata((BitAiAttachment)o, (IReadOnlyDictionary<string, object?>?)v)),
    ];

    private static void ReadLikeReflection(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind is not JsonTypeInfoKind.Object) return;

        // The required members are assigned a placeholder only to create the record: the JSON sets them, and the
        // serializer still fails a JSON that leaves one out, since the properties stay marked as required.
        if (typeInfo.Type == typeof(BitAiConversationSnapshot)) Apply(typeInfo, static () => new BitAiConversationSnapshot { ConversationId = null! }, ConversationMembers);
        else if (typeInfo.Type == typeof(BitAiMessageSnapshot)) Apply(typeInfo, static () => new BitAiMessageSnapshot { MessageId = null! }, MessageMembers);
        else if (typeInfo.Type == typeof(BitAiArtifactSnapshot)) Apply(typeInfo, static () => new BitAiArtifactSnapshot { ArtifactId = null! }, ArtifactMembers);
        else if (typeInfo.Type == typeof(BitAiAgentRunSnapshot)) Apply(typeInfo, static () => new BitAiAgentRunSnapshot { RunId = null! }, RunMembers);
        else if (typeInfo.Type == typeof(BitAiAgentStepSnapshot)) Apply(typeInfo, static () => new BitAiAgentStepSnapshot { StepId = null! }, StepMembers);
        else if (typeInfo.Type == typeof(BitAiAttachment) && Apply(typeInfo, static () => CreateAttachment(), AttachmentMembers))
        {
            // What the public constructor checks, which the JSON no longer goes through.
            typeInfo.OnDeserialized = static o =>
            {
                var attachment = (BitAiAttachment)o;
                ArgumentException.ThrowIfNullOrWhiteSpace(attachment.AttachmentId, "attachmentId");
                ArgumentNullException.ThrowIfNull(attachment.Name, "name");
            };
        }
    }

    /// <summary>
    /// Creates the record through <paramref name="create"/> and sets each property through its init accessor. The JSON
    /// names follow the naming policy of the settings, as the names of the properties do. An ignored property (which the
    /// generated contract neither reads nor writes) and a computed one (which nothing can set) are left as they are; any
    /// other this does not know (one added to a record without adding it here) leaves the generated contract whole
    /// rather than half replaced.
    /// </summary>
    /// <returns>Whether the contract was replaced.</returns>
    private static bool Apply(JsonTypeInfo typeInfo, Func<object> create, Member[] members)
    {
        var naming = typeInfo.Options.PropertyNamingPolicy;
        var setters = new Action<object, object?>?[typeInfo.Properties.Count];

        for (var i = 0; i < setters.Length; i++)
        {
            var property = typeInfo.Properties[i];
            if (property.Get is null && property.Set is null) continue;

            var member = Array.Find(members, m => string.Equals(naming?.ConvertName(m.Name) ?? m.Name, property.Name, StringComparison.Ordinal));
            if (member is null && property.Set is not null) return false;
            setters[i] = member?.Set;
        }

        for (var i = 0; i < setters.Length; i++)
        {
            if (setters[i] is { } set) typeInfo.Properties[i].Set = set;
        }

        typeInfo.CreateObject = create;
        return true;
    }

    private sealed record Member(string Name, Action<object, object?> Set);

    private const string InitAccessor = "set_";

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiConversationSnapshot.SchemaVersion))]
    private static extern void SetSchemaVersion(BitAiConversationSnapshot target, int value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiConversationSnapshot.ConversationId))]
    private static extern void SetConversationId(BitAiConversationSnapshot target, string value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiConversationSnapshot.Title))]
    private static extern void SetTitle(BitAiConversationSnapshot target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiConversationSnapshot.CreatedAt))]
    private static extern void SetCreatedAt(BitAiConversationSnapshot target, DateTimeOffset value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiConversationSnapshot.UpdatedAt))]
    private static extern void SetUpdatedAt(BitAiConversationSnapshot target, DateTimeOffset value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiConversationSnapshot.Messages))]
    private static extern void SetMessages(BitAiConversationSnapshot target, IReadOnlyList<BitAiMessageSnapshot> value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiConversationSnapshot.Artifacts))]
    private static extern void SetArtifacts(BitAiConversationSnapshot target, IReadOnlyList<BitAiArtifactSnapshot> value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiConversationSnapshot.Runs))]
    private static extern void SetRuns(BitAiConversationSnapshot target, IReadOnlyList<BitAiAgentRunSnapshot> value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiConversationSnapshot.Extensions))]
    private static extern void SetExtensions(BitAiConversationSnapshot target, IReadOnlyDictionary<string, JsonElement>? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.MessageId))]
    private static extern void SetMessageId(BitAiMessageSnapshot target, string value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.Role))]
    private static extern void SetRole(BitAiMessageSnapshot target, BitAiRole value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.CreatedAt))]
    private static extern void SetCreatedAt(BitAiMessageSnapshot target, DateTimeOffset value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.Status))]
    private static extern void SetStatus(BitAiMessageSnapshot target, BitAiMessageStatus value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.Metadata))]
    private static extern void SetMetadata(BitAiMessageSnapshot target, BitAiGenerationMetadata? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.Error))]
    private static extern void SetError(BitAiMessageSnapshot target, BitAiError? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.Feedback))]
    private static extern void SetFeedback(BitAiMessageSnapshot target, BitAiFeedbackKind value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.FeedbackComment))]
    private static extern void SetFeedbackComment(BitAiMessageSnapshot target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.RunId))]
    private static extern void SetRunId(BitAiMessageSnapshot target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.Parts))]
    private static extern void SetParts(BitAiMessageSnapshot target, IReadOnlyList<BitAiPartSnapshot> value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.Attachments))]
    private static extern void SetAttachments(BitAiMessageSnapshot target, IReadOnlyList<BitAiAttachment> value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.Citations))]
    private static extern void SetCitations(BitAiMessageSnapshot target, IReadOnlyList<BitAiCitation> value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiMessageSnapshot.Extensions))]
    private static extern void SetExtensions(BitAiMessageSnapshot target, IReadOnlyDictionary<string, JsonElement>? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.ArtifactId))]
    private static extern void SetArtifactId(BitAiArtifactSnapshot target, string value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.Kind))]
    private static extern void SetKind(BitAiArtifactSnapshot target, BitAiArtifactKind value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.Title))]
    private static extern void SetTitle(BitAiArtifactSnapshot target, string value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.Content))]
    private static extern void SetContent(BitAiArtifactSnapshot target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.Language))]
    private static extern void SetLanguage(BitAiArtifactSnapshot target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.Url))]
    private static extern void SetUrl(BitAiArtifactSnapshot target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.Version))]
    private static extern void SetVersion(BitAiArtifactSnapshot target, int value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.IsStreaming))]
    private static extern void SetIsStreaming(BitAiArtifactSnapshot target, bool value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.CreatedAt))]
    private static extern void SetCreatedAt(BitAiArtifactSnapshot target, DateTimeOffset value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.UpdatedAt))]
    private static extern void SetUpdatedAt(BitAiArtifactSnapshot target, DateTimeOffset value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.Versions))]
    private static extern void SetVersions(BitAiArtifactSnapshot target, IReadOnlyList<BitAiArtifactVersion> value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiArtifactSnapshot.Metadata))]
    private static extern void SetMetadata(BitAiArtifactSnapshot target, IReadOnlyDictionary<string, JsonElement>? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentRunSnapshot.RunId))]
    private static extern void SetRunId(BitAiAgentRunSnapshot target, string value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentRunSnapshot.AgentName))]
    private static extern void SetAgentName(BitAiAgentRunSnapshot target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentRunSnapshot.Goal))]
    private static extern void SetGoal(BitAiAgentRunSnapshot target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentRunSnapshot.Status))]
    private static extern void SetStatus(BitAiAgentRunSnapshot target, BitAiAgentRunStatus value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentRunSnapshot.PendingPrompt))]
    private static extern void SetPendingPrompt(BitAiAgentRunSnapshot target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentRunSnapshot.PendingSuggestions))]
    private static extern void SetPendingSuggestions(BitAiAgentRunSnapshot target, IReadOnlyList<string>? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentRunSnapshot.StartedAt))]
    private static extern void SetStartedAt(BitAiAgentRunSnapshot target, DateTimeOffset value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentRunSnapshot.CompletedAt))]
    private static extern void SetCompletedAt(BitAiAgentRunSnapshot target, DateTimeOffset? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentRunSnapshot.Usage))]
    private static extern void SetUsage(BitAiAgentRunSnapshot target, BitAiTokenUsage value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentRunSnapshot.Error))]
    private static extern void SetError(BitAiAgentRunSnapshot target, BitAiError? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentRunSnapshot.Steps))]
    private static extern void SetSteps(BitAiAgentRunSnapshot target, IReadOnlyList<BitAiAgentStepSnapshot> value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentStepSnapshot.StepId))]
    private static extern void SetStepId(BitAiAgentStepSnapshot target, string value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentStepSnapshot.Title))]
    private static extern void SetTitle(BitAiAgentStepSnapshot target, string value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentStepSnapshot.Detail))]
    private static extern void SetDetail(BitAiAgentStepSnapshot target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentStepSnapshot.Status))]
    private static extern void SetStatus(BitAiAgentStepSnapshot target, BitAiAgentStepStatus value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentStepSnapshot.Summary))]
    private static extern void SetSummary(BitAiAgentStepSnapshot target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentStepSnapshot.Error))]
    private static extern void SetError(BitAiAgentStepSnapshot target, BitAiError? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentStepSnapshot.StartedAt))]
    private static extern void SetStartedAt(BitAiAgentStepSnapshot target, DateTimeOffset? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentStepSnapshot.CompletedAt))]
    private static extern void SetCompletedAt(BitAiAgentStepSnapshot target, DateTimeOffset? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAgentStepSnapshot.ToolCallIds))]
    private static extern void SetToolCallIds(BitAiAgentStepSnapshot target, IReadOnlyList<string> value);

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    private static extern BitAiAttachment CreateAttachment();

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.AttachmentId))]
    private static extern void SetAttachmentId(BitAiAttachment target, string value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.Name))]
    private static extern void SetName(BitAiAttachment target, string value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.ContentType))]
    private static extern void SetContentType(BitAiAttachment target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.Size))]
    private static extern void SetSize(BitAiAttachment target, long? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.Kind))]
    private static extern void SetKind(BitAiAttachment target, BitAiAttachmentKind value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.Status))]
    private static extern void SetStatus(BitAiAttachment target, BitAiAttachmentStatus value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.Progress))]
    private static extern void SetProgress(BitAiAttachment target, double? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.Error))]
    private static extern void SetError(BitAiAttachment target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.Url))]
    private static extern void SetUrl(BitAiAttachment target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.ThumbnailUrl))]
    private static extern void SetThumbnailUrl(BitAiAttachment target, string? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.Data))]
    private static extern void SetData(BitAiAttachment target, ReadOnlyMemory<byte>? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = InitAccessor + nameof(BitAiAttachment.Metadata))]
    private static extern void SetMetadata(BitAiAttachment target, IReadOnlyDictionary<string, object?>? value);
}
