using Bit.CodeAnalyzers.SystemAnalyzers;
using Bit.CodeAnalyzers.SystemCodeFixes;
using static Bit.CodeAnalyzers.Tests.AnalyzerTestHarness;

namespace Bit.CodeAnalyzers.Tests;

[TestClass]
public class ClassWithoutModifierCodeFixProviderTests
{
    private static async Task AssertFix(string source, string expected)
    {
        var actual = await ApplyCodeFix(new ClassWithoutModifierAnalyzer(), new ClassWithoutModifierCodeFixProvider(), source);

        Assert.AreEqual(expected.ReplaceLineEndings(), actual.ReplaceLineEndings());
    }

    [TestMethod]
    public async Task AddsPublicToAClass()
    {
        await AssertFix("""
            namespace App;

            class Helper
            {
            }
            """, """
            namespace App;

            public class Helper
            {
            }
            """);
    }

    [TestMethod]
    public async Task KeepsTheDocumentationCommentAndIndentationAboveTheModifier()
    {
        await AssertFix("""
            namespace App
            {
                public class Outer
                {
                    /// <summary>
                    /// The state.
                    /// </summary>
                    class State
                    {
                    }
                }
            }
            """, """
            namespace App
            {
                public class Outer
                {
                    /// <summary>
                    /// The state.
                    /// </summary>
                    public class State
                    {
                    }
                }
            }
            """);
    }

    [TestMethod]
    public async Task KeepsPreprocessorDirectivesAboveTheModifier()
    {
        await AssertFix("""
            namespace App;

            #region Helpers
            class Helper
            {
            }
            #endregion
            """, """
            namespace App;

            #region Helpers
            public class Helper
            {
            }
            #endregion
            """);
    }

    [TestMethod]
    public async Task PutsTheModifierAfterTheAttributes()
    {
        await AssertFix("""
            namespace App
            {
                [System.Serializable]
                class Helper
                {
                }
            }
            """, """
            namespace App
            {
                [System.Serializable]
                public class Helper
                {
                }
            }
            """);
    }

    [TestMethod]
    public async Task FixesRecords()
    {
        await AssertFix("""
            namespace App;

            // A person.
            record class Person(string Name);
            """, """
            namespace App;

            // A person.
            public record class Person(string Name);
            """);
    }

    [TestMethod]
    public async Task FixAllFixesNestedAndSiblingClasses()
    {
        var actual = await ApplyFixAll(new ClassWithoutModifierAnalyzer(), new ClassWithoutModifierCodeFixProvider(), """
            namespace App;

            class Outer
            {
                class Inner
                {
                }
            }

            /// <summary>Sibling.</summary>
            class Sibling
            {
            }
            """);

        Assert.AreEqual("""
            namespace App;

            public class Outer
            {
                public class Inner
                {
                }
            }

            /// <summary>Sibling.</summary>
            public class Sibling
            {
            }
            """.ReplaceLineEndings(), actual.ReplaceLineEndings());
    }
}
