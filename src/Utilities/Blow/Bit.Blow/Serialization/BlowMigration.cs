using System.Text.Json.Nodes;

namespace Bit.Blow;

/// <summary>
/// One step of bringing an older document up to date: <see cref="Migrate"/> receives a document of
/// <see cref="FromVersion"/> as JSON and returns it as version <c>FromVersion + 1</c> would have written it.
/// </summary>
/// <param name="FromVersion">The application document version this step upgrades from.</param>
/// <param name="Migrate">The upgrade. It may change the object it is given and return it.</param>
public sealed record BlowMigration(int FromVersion, Func<JsonObject, JsonObject> Migrate);
