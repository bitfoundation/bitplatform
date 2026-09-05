using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// The text, clone, URL and selection wrappers, driven through the deterministic harness. All four
/// are implemented by every engine and need no permission, so they assert exact values rather than
/// feature-gating - a wrong answer here is a real regression, not a browser difference.
/// </summary>
[TestClass]
public class TextUrlAndSelectionTests : ButilPageTest
{
    [TestMethod]
    public async Task TextEncoding_Decodes_A_Legacy_Code_Page()
    {
        // The bytes are Shift_JIS, which .NET on WebAssembly cannot decode without an extra
        // package - so this asserts the whole reason the wrapper exists.
        await ClickAndExpectAsync("text-decode", "text:decode:こんにちは/shift_jis");
    }

    [TestMethod]
    public async Task TextEncoding_Encodes_Utf8_And_Measures_It()
    {
        // "héllo" is five characters and six UTF-8 bytes; both numbers have to say six.
        await ClickAndExpectAsync("text-encode", "text:encode:6/6");
    }

    [TestMethod]
    public async Task TextEncoding_Streaming_Decoder_Survives_A_Split_Character()
    {
        // The chunks split through the middle of a two-byte character. A per-chunk decode would
        // produce replacement characters; the streaming decoder has to match the whole-buffer read.
        await ClickAndExpectAsync("text-stream", "text:stream:True");
    }

    [TestMethod]
    public async Task StructuredClone_Round_Trips_A_Payload()
    {
        await ClickAndExpectAsync("clone-roundtrip", "clone:roundtrip:42/answer");
    }

    [TestMethod]
    public async Task StructuredClone_Accepts_A_Plain_Payload()
    {
        await ClickAndExpectAsync("clone-can", "clone:can:True");
    }

    [TestMethod]
    public async Task Url_Parses_Into_Components()
    {
        await ClickAndExpectAsync("url-parse", "url:parse:https://example.com:8080|/docs/guide|8080|#top");
    }

    [TestMethod]
    public async Task Url_Keeps_Repeated_Query_Values_And_Sorts()
    {
        await ClickAndExpectAsync("url-query", "url:query:2/https://example.com/x?a=1&b=2");
    }

    [TestMethod]
    public async Task UrlPattern_Captures_A_Named_Group()
    {
        // Chromium has URLPattern, so the harness's unsupported branch should not be reached here.
        await ClickAndExpectAsync("url-pattern", "url:pattern:42");
    }

    [TestMethod]
    public async Task UrlPattern_Reports_Credentials()
    {
        await ClickAndExpectAsync("url-pattern-credentials", "url:credentials:ada/secret");
    }

    [TestMethod]
    public async Task Selection_Selects_An_Elements_Contents()
    {
        await ClickAndExpectAsync("sel-select", "sel:select:butil selection target");
    }

    [TestMethod]
    public async Task Selection_Selects_A_Character_Range()
    {
        await ClickAndExpectAsync("sel-range", "sel:range:butil");
    }

    [TestMethod]
    public async Task Selection_Reports_Offsets_Within_An_Element()
    {
        await ClickAndExpectAsync("sel-offsets", "sel:offsets:6/15");
    }

    [TestMethod]
    public async Task Selection_Reports_Offsets_Of_A_Whole_Element()
    {
        // selectNodeContents leaves both boundaries on the element itself. Reading them back has to
        // say "all of the text", not the caret-at-the-end an element boundary looks like.
        await ClickAndExpectAsync("sel-content-offsets", "sel:contents:0/22");
    }

    [TestMethod]
    public async Task Selection_Places_A_Caret_In_An_Element_With_No_Text()
    {
        // Counting offsets over text nodes leaves an empty element with none to land on, but the
        // start of it is a valid caret position - and the one GetRangeIn reports for it.
        await ClickAndExpectAsync("sel-empty-range", "sel:empty:True/0/0");
    }

    [TestMethod]
    public async Task Selection_Refuses_Negative_Offsets()
    {
        // Both of these would reach Range.setStart with a negative offset, which throws.
        await ClickAndExpectAsync("sel-bad-range", "sel:badrange:False/False");
    }

    [TestMethod]
    public async Task Selection_Replaces_And_Leaves_A_Caret_After_The_Text()
    {
        await ClickAndExpectAsync("sel-replace", "sel:replace:True/BUTIL selection target");
    }

    [TestMethod]
    public async Task Selection_Clears()
    {
        await ClickAndExpectAsync("sel-clear", "sel:clear:True");
    }
}
