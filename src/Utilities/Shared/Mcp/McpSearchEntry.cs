namespace Bit.Utilities.Mcp;

/// <summary>One thing an MCP server's search can answer with, and the call that returns it in full.</summary>
/// <param name="Kind">What it is - "Guide section", "API method", "Source file" - so a caller can tell a reference from an example.</param>
/// <param name="Title">What it is listed as, and the words a term is counted in before it is counted in prose.</param>
/// <param name="Context">Where it sits: the section a section is under, the type a member is on.</param>
/// <param name="Tool">
/// The exact call that returns the whole of it. Entries read through the same call under the same title are one
/// hit, and so are all the entries read through a call that takes no argument: it answers the same text whichever
/// of them led to it.
/// </param>
/// <param name="Body">Its prose, which counts least, up to a ceiling, and which a hit's snippet is cut from.</param>
/// <param name="Boosted">
/// Words that are as good as its name without being its name: a member's signature, a diagnostic's id, a section's
/// extra vocabulary. They are a list of names, where saying a word twice does not make it more the entry's, so a
/// word counts there once however often it is said.
/// </param>
/// <param name="Weight">How much an answer this kind of entry is: <see cref="McpSearch.ReferenceWeight"/> or <see cref="McpSearch.ExampleWeight"/>.</param>
/// <param name="Name">
/// What the entry is called, when that is less than its title: a member's own name, without the name of the type it
/// is a member of. The type's name says where the member lives, which every other member of the type shares -
/// counted as the member's name, "query" would name every member of a type called ...Query, and every question with
/// "query" in it would be answered with them. In the title it still counts, as any other word of a title does.
/// </param>
/// <param name="Aliases">
/// The words a caller uses for it without it using them: someone who wants to "encrypt the database" is asking for
/// "Encryption at rest". They count as its name, so they rank it where its name would.
/// </param>
internal sealed record McpSearchEntry(string Kind, string Title, string? Context, string Tool, string Body, string Boosted,
                                      int Weight = McpSearch.ReferenceWeight, string? Name = null, string? Aliases = null);
