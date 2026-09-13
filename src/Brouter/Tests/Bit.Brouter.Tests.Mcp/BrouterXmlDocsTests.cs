using System.Reflection;
using System.Xml.Linq;
using Bit.Brouter.Demo.Server.Services;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The XML documentation the Bit.Brouter build emits, flattened into the prose the API tools serve.
/// <para>
/// It is the same text a developer reads in IntelliSense, which is what makes it the most accurate
/// answer available about a member - there is no second copy to keep in sync. The flattening is
/// where that can go wrong: a cref left as <c>T:Bit.Brouter.IBrouter</c>, a code sample whose line
/// breaks were eaten by the prose-unwrapping pass, an empty answer because the .xml file did not
/// travel with the assembly.
/// </para>
/// </summary>
[TestClass]
public class BrouterXmlDocsTests
{
    [TestMethod]
    public void The_documentation_file_is_found_next_to_the_assembly()
    {
        // It is emitted into the build output and has to be copied wherever the app runs. When it is
        // not, every lookup here returns null and the API reference silently degrades to a list of names.
        var summary = BrouterXmlDocs.GetSummary("T:Bit.Brouter.IBrouter");

        Assert.IsFalse(string.IsNullOrWhiteSpace(summary),
            "No documentation was found for IBrouter - Bit.Brouter.xml is not next to the assembly at run time.");
    }

    [TestMethod]
    public void A_cref_is_flattened_to_the_name_a_reader_would_say()
    {
        var summary = BrouterXmlDocs.GetSummary("T:Bit.Brouter.Broute")!;

        // "Declares a single route inside a <see cref="T:Bit.Brouter.Brouter"/>."
        StringAssert.Contains(summary, "Brouter");
        Assert.IsFalse(summary.Contains("T:Bit.Brouter", StringComparison.Ordinal), $"A raw cref reached the reader: {summary}");
        Assert.IsFalse(summary.Contains("<see", StringComparison.Ordinal));
    }

    [TestMethod]
    public void No_flattened_documentation_carries_markup_through()
    {
        foreach (var id in new[]
        {
            "T:Bit.Brouter.BrouterOptions",
            "P:Bit.Brouter.BrouterOptions.ScrollBehavior",
            "P:Bit.Brouter.Broute.KeepAlive",
            "T:Bit.Brouter.BrouterConstraintRegistry",
        })
        {
            foreach (var text in new[] { BrouterXmlDocs.GetSummary(id), BrouterXmlDocs.GetRemarks(id) })
            {
                if (text is null) continue;

                foreach (var tag in new[] { "<para>", "<c>", "<see ", "<paramref", "<summary>", "<remarks>" })
                {
                    Assert.IsFalse(text.Contains(tag, StringComparison.Ordinal), $"'{id}' still carries {tag} in its text.");
                }
            }
        }
    }

    [TestMethod]
    public void An_inline_code_span_arrives_as_the_code_it_shows()
    {
        // <c>&lt;BrouterOutlet Name="..."&gt;</c> is markup wrapped around escaped markup; what a
        // reader needs is the element itself.
        var summary = BrouterXmlDocs.GetSummary("T:Bit.Brouter.BrouterView")!;

        StringAssert.Contains(summary, "<BrouterOutlet Name=\"...\">");
        Assert.IsFalse(summary.Contains("&lt;", StringComparison.Ordinal), "XML entities reached the reader unescaped.");
    }

    [TestMethod]
    public void A_paragraph_break_survives_while_a_wrapped_line_does_not()
    {
        var remarks = BrouterXmlDocs.GetRemarks("T:Bit.Brouter.BrouterConstraintRegistry")!;

        StringAssert.Contains(remarks, "\n\n", "The <para> breaks were flattened away, so the remarks are one wall of text.");
        Assert.IsFalse(remarks.Contains("  ", StringComparison.Ordinal), "The source's line wrapping left runs of spaces behind.");
    }

    [TestMethod]
    public void A_member_that_is_not_documented_answers_with_nothing_rather_than_with_an_error()
    {
        Assert.IsNull(BrouterXmlDocs.GetSummary("T:Bit.Brouter.NoSuchType"));
        Assert.IsNull(BrouterXmlDocs.GetRemarks("M:Bit.Brouter.IBrouter.NoSuchMethod"));
        Assert.IsNull(BrouterXmlDocs.GetSummary(string.Empty));
    }

    [TestMethod]
    public void A_method_id_without_its_parameter_list_still_finds_an_overload()
    {
        // Overloads are told apart by their parameter list. When building one did not produce an
        // exact hit - a generic, a modifier, a type spelled differently - one overload's
        // documentation still beats none.
        var exact = BrouterXmlDocs.GetSummary("M:Bit.Brouter.IBrouter.NavigateAsync(System.String,System.Boolean,System.String)");
        Assert.IsFalse(string.IsNullOrWhiteSpace(exact));

        var inexact = BrouterXmlDocs.GetSummary("M:Bit.Brouter.IBrouter.NavigateAsync");
        Assert.AreEqual(exact, inexact, "An id without its parameter list found no overload's documentation.");
    }

    [TestMethod]
    public void The_same_id_answers_the_same_way_every_time()
    {
        // Which overload stands in for an inexact id is decided by ordering the candidates: a frozen
        // dictionary enumerates in whatever order it hashed into, so taking one off it directly would
        // answer differently per build. Asking twice cannot see that - the table is built once per
        // process, so both answers come off the same hashing - which leaves the rule itself as the
        // only thing worth checking: every stand-in is the ordinally first of its candidates.
        var members = Table("_members");
        var overloads = Table("_overloads");

        Assert.IsTrue(members.Count > 0, "No documentation was loaded, so nothing here is being decided.");
        Assert.IsTrue(overloads.Count > 0, "No method id has a stand-in, so an inexact id finds nothing.");

        foreach (var (name, chosen) in overloads)
        {
            var candidates = members.Keys.Where(id => id.StartsWith($"{name}(", StringComparison.Ordinal))
                                         .OrderBy(id => id, StringComparer.Ordinal)
                                         .ToArray();

            // No documented method in Bit.Brouter has two overloads yet, so today every group has
            // one candidate and this cannot catch a dropped OrderBy on its own. It is the rule that
            // is written down here; it starts biting the day an overload is documented.
            Assert.AreNotEqual(0, candidates.Length, $"'{name}' stands in for no member id at all.");
            Assert.AreSame(members[candidates[0]], chosen,
                           $"'{name}' answers with something other than the ordinally first of its {candidates.Length} candidates.");
        }

        // And the ordering has to be the one callers actually reach, not one the test computed for
        // itself: the inexact id's answer is the ordinally first overload's summary.
        const string inexactId = "M:Bit.Brouter.IBrouter.NavigateAsync";

        var expected = members.Keys.Where(id => id.StartsWith($"{inexactId}(", StringComparison.Ordinal))
                                   .OrderBy(id => id, StringComparer.Ordinal)
                                   .First();

        Assert.AreEqual(BrouterXmlDocs.GetSummary(expected), BrouterXmlDocs.GetSummary(inexactId));

        static IReadOnlyDictionary<string, XElement> Table(string field)
        {
            var lazy = typeof(BrouterXmlDocs).GetField(field, BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);

            Assert.IsNotNull(lazy, $"BrouterXmlDocs.{field} has been renamed or removed; the ordering rule cannot be checked.");

            return (IReadOnlyDictionary<string, XElement>)lazy.GetType().GetProperty("Value")!.GetValue(lazy)!;
        }
    }
}
