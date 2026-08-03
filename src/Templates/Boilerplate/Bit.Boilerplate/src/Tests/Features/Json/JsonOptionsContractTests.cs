using Boilerplate.Server.Api.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Json;

/// <summary>
/// The four JSON option shapes in the template - <see cref="AppJsonContext"/>, <see cref="IdentityJsonContext"/>,
/// <see cref="ServerJsonContext"/> and <c>JsonSerializerOptionsExtensions.ApplyDefaultOptions</c> - are kept in step
/// by a comment ("Changes in this file should be applied on ... as well") and nothing else. A property added to one
/// and forgotten on the others produces a wire or storage format that differs by code path, which no compiler and no
/// happy-path test notices.
/// <para>
/// This pins the one property that has already cost a user-visible bug, plus the round trip it broke.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class JsonOptionsContractTests
{
    /// <summary>
    /// <b>Dictionary keys are data, not property names.</b> <c>DictionaryKeyPolicy</c> applies on write only - STJ
    /// never reverses it on read - so any policy at all makes a dictionary round trip lossy the moment a key is not
    /// already in the policy's own casing.
    /// <para>
    /// The concrete cost: <c>WindowsStorageService</c> persists its whole key/value store as a
    /// <c>Dictionary&lt;string, string?&gt;</c> through <see cref="AppJsonContext"/>. With a camelCase key policy the
    /// key written as <c>"Culture"</c> came back as <c>"culture"</c>, <c>GetItem("Culture")</c> returned null, and the
    /// Windows client discarded the user's chosen language on <b>every</b> restart - while the next write appended a
    /// second <c>culture</c> member to the file. Nothing in the template needs a key policy: extension-data keys are
    /// exempt from it and <c>Dictionary&lt;string, JsonElement&gt;</c> (JWT claims) is read-only.
    /// </para>
    /// </summary>
    [TestMethod]
    public void NoJsonOptionShape_Should_ApplyADictionaryKeyPolicy()
    {
        var applyDefaultOptions = new JsonSerializerOptions();
        applyDefaultOptions.ApplyDefaultOptions();

        (string Name, JsonSerializerOptions Options)[] shapes =
        [
            (nameof(AppJsonContext), AppJsonContext.Default.Options),
            (nameof(IdentityJsonContext), IdentityJsonContext.Default.Options),
            (nameof(ServerJsonContext), ServerJsonContext.Default.Options),
            ("ApplyDefaultOptions", applyDefaultOptions)
        ];

        foreach (var (name, options) in shapes)
        {
            Assert.IsNull(options.DictionaryKeyPolicy,
                $"{name} applies a DictionaryKeyPolicy. It rewrites keys on write and never undoes it on read, so every " +
                "dictionary this serializes stops round-tripping - which is how the Windows client lost the saved culture " +
                "on every restart. Property naming (PropertyNamingPolicy) is the separate, intended setting.");
        }
    }

    /// <summary>
    /// The behaviour the assertion above exists for, driven exactly as <c>WindowsStorageService.Save</c> /
    /// <c>Restore</c> drive it. A key policy passes the assertion's sibling checks and still fails this one, which is
    /// why both are here.
    /// </summary>
    [TestMethod]
    public void ThePersistedStorageDictionary_Should_RoundTripItsKeysVerbatim()
    {
        // The real keys the clients store; "Culture" is the one whose casing does not survive a camelCase key policy.
        var saved = new Dictionary<string, string?>
        {
            ["access_token"] = "at",
            ["refresh_token"] = "rt",
            ["Culture"] = "fa-IR",
            ["bit-webauthn"] = "[]"
        };

        var json = JsonSerializer.Serialize(saved, AppJsonContext.Default.DictionaryStringString);
        var restored = JsonSerializer.Deserialize(json, AppJsonContext.Default.DictionaryStringString)!;

        Assert.AreEqual("fa-IR", restored.GetValueOrDefault("Culture"),
            $"The key the app wrote must be the key the app reads back. Written json: {json}");

        Assert.AreSequenceEqual(saved.Keys.ToArray(), restored.Keys.ToArray(), Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, $"No key may be rewritten on the way to disk. Written json: {json}");
    }

    /// <summary>
    /// The property-name half of the same contract, asserted so removing the key policy cannot be "simplified" into
    /// removing the naming policy: every DTO on the wire is camelCase, and the client and server agree only because
    /// all four shapes say so.
    /// </summary>
    [TestMethod]
    public void EveryJsonOptionShape_Should_AgreeOnCamelCasePropertyNames()
    {
        var applyDefaultOptions = new JsonSerializerOptions();
        applyDefaultOptions.ApplyDefaultOptions();

        foreach (var (name, options) in new (string, JsonSerializerOptions)[]
        {
            (nameof(AppJsonContext), AppJsonContext.Default.Options),
            (nameof(IdentityJsonContext), IdentityJsonContext.Default.Options),
            (nameof(ServerJsonContext), ServerJsonContext.Default.Options),
            ("ApplyDefaultOptions", applyDefaultOptions)
        })
        {
            Assert.AreSame(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy,
                $"{name} must name properties in camelCase like the other three, or the same DTO gets two wire formats.");
            Assert.IsTrue(options.PropertyNameCaseInsensitive, $"{name} must read properties case-insensitively like the other three.");
            Assert.IsTrue(options.AllowTrailingCommas, $"{name} must allow trailing commas like the other three.");
        }
    }
}
