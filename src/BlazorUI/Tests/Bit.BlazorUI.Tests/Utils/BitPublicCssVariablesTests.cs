using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

[TestClass]
public sealed class BitPublicCssVariablesTests
{
    private const string PREFIX = "--bit-Foo-";

    private static string? Prepend(string? style, string? stylesRoot = null, string? partStyle = null)
    {
        return new BitPublicCssVariables(PREFIX).Prepend(style, stylesRoot, partStyle);
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldCopyOnlyTheDeclarationsOfItsPrefix()
    {
        // Nothing but the public variables of the component travels: neither a plain declaration nor the
        // variables of another component.
        Assert.AreEqual("--bit-Foo-a:1;--bit-Foo-b:2;",
                        Prepend("margin:1rem;--bit-Foo-a:1;--bit-Bar-a:3;--bit-Foo-b:2"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldPutTheStyleBeforeStylesRootAndBothBeforeThePartStyle()
    {
        // The part's own style comes last so that it still wins over what is copied onto it.
        Assert.AreEqual("--bit-Foo-a:1;--bit-Foo-b:2;padding:2px",
                        Prepend("--bit-Foo-a:1", "--bit-Foo-b:2", "padding:2px"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldReturnThePartStyleWhenThereIsNothingToCopy()
    {
        Assert.AreEqual("padding:2px", Prepend("margin:1rem", null, "padding:2px"));
        Assert.AreEqual("padding:2px", Prepend(null, null, "padding:2px"));
        Assert.IsNull(Prepend("margin:1rem"));
        Assert.IsNull(Prepend(null));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldReturnTheVariablesAloneWhenThereIsNoPartStyle()
    {
        Assert.AreEqual("--bit-Foo-a:1;", Prepend("--bit-Foo-a:1", null, null));
        Assert.AreEqual("--bit-Foo-a:1;", Prepend("--bit-Foo-a:1", null, ""));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldTrimTheDeclarationsAndSkipTheEmptyOnes()
    {
        Assert.AreEqual("--bit-Foo-a:1;--bit-Foo-b:2;",
                        Prepend(" ;  --bit-Foo-a:1 ;;   --bit-Foo-b:2  ; "));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldEndATrailingDeclarationWithASemicolon()
    {
        // The last declaration of a style needs no semicolon of its own, but the part's style is written right
        // after it, so the copy always ends one.
        Assert.AreEqual("--bit-Foo-a:1;--bit-Foo-b:2;padding:2px",
                        Prepend("--bit-Foo-a:1;--bit-Foo-b:2", null, "padding:2px"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldKeepASemicolonInsideBrackets()
    {
        Assert.AreEqual("--bit-Foo-a:url(data:image/png;base64,AAA);--bit-Foo-b:[a;b];--bit-Foo-c:{x;y};",
                        Prepend("--bit-Foo-a:url(data:image/png;base64,AAA);--bit-Foo-b:[a;b];--bit-Foo-c:{x;y}"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldKeepASemicolonInsideNestedBrackets()
    {
        // The declaration ends only once every bracket opened in it has closed, not at the first closing one.
        Assert.AreEqual("--bit-Foo-a:image-set(url(a;b) 1x, url(c;d) 2x);--bit-Foo-b:2;",
                        Prepend("--bit-Foo-a:image-set(url(a;b) 1x, url(c;d) 2x);color:red;--bit-Foo-b:2"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldKeepASemicolonInsideEitherKindOfQuote()
    {
        Assert.AreEqual("--bit-Foo-a:'a;b';--bit-Foo-b:\"c;d\";",
                        Prepend("--bit-Foo-a:'a;b';--bit-Foo-b:\"c;d\""));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldNotEndAQuoteAtTheOtherKindOfQuote()
    {
        Assert.AreEqual("--bit-Foo-a:'say \"hi;\" twice';--bit-Foo-b:\"it's;ok\";",
                        Prepend("--bit-Foo-a:'say \"hi;\" twice';--bit-Foo-b:\"it's;ok\""));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldNotCountABracketInsideAQuote()
    {
        // A bracket written in a string opens nothing, so the semicolon after the string still ends the declaration.
        Assert.AreEqual("--bit-Foo-a:'(';--bit-Foo-b:2;",
                        Prepend("--bit-Foo-a:'(';color:red;--bit-Foo-b:2"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldSkipAnEscapedCharacter()
    {
        // An escaped semicolon is part of the value, and an escaped quote neither opens a string nor closes one.
        Assert.AreEqual(@"--bit-Foo-a:a\;b;--bit-Foo-b:'it\'s;ok';--bit-Foo-c:\';",
                        Prepend(@"--bit-Foo-a:a\;b;--bit-Foo-b:'it\'s;ok';--bit-Foo-c:\';color:red"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldCopyAnUnclosedBracketAsItIsWithoutThrowing()
    {
        // A bracket that is never closed takes the rest of the style into its declaration, which is what a browser
        // makes of it as well; the copy neither throws nor drops it.
        Assert.AreEqual("--bit-Foo-a:url(a;b;--bit-Foo-b:2;",
                        Prepend("--bit-Foo-a:url(a;b;--bit-Foo-b:2"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldIgnoreAStrayClosingBracket()
    {
        Assert.AreEqual("--bit-Foo-a:a);--bit-Foo-b:2;",
                        Prepend("--bit-Foo-a:a);--bit-Foo-b:2"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldCopyAnUnclosedQuoteAsItIsWithoutThrowing()
    {
        Assert.AreEqual("--bit-Foo-a:'a;--bit-Foo-b:2;",
                        Prepend("--bit-Foo-a:'a;--bit-Foo-b:2"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldCopyATrailingBackslashAsItIsWithoutThrowing()
    {
        Assert.AreEqual(@"--bit-Foo-a:a\;",
                        Prepend(@"--bit-Foo-a:a\"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldOnlyCopyADeclarationWhoseNameStartsWithThePrefix()
    {
        // The prefix written inside the value of another declaration is not a public variable of the component.
        Assert.AreEqual("padding:2px",
                        Prepend("--x:var(--bit-Foo-a);color:var(--bit-Foo-b)", null, "padding:2px"));
    }

    [TestMethod]
    public void BitPublicCssVariablesShouldFollowAChangeOfEitherInput()
    {
        // One instance remembers what it last picked out; a change of the Style or of Styles.Root is picked out
        // again, while a change of the part's own style alone is not a reason to.
        var variables = new BitPublicCssVariables(PREFIX);

        Assert.AreEqual("--bit-Foo-a:1;padding:1px", variables.Prepend("--bit-Foo-a:1", null, "padding:1px"));
        Assert.AreEqual("--bit-Foo-a:1;padding:2px", variables.Prepend("--bit-Foo-a:1", null, "padding:2px"));
        Assert.AreEqual("--bit-Foo-a:2;padding:2px", variables.Prepend("--bit-Foo-a:2", null, "padding:2px"));
        Assert.AreEqual("--bit-Foo-a:2;--bit-Foo-b:3;padding:2px", variables.Prepend("--bit-Foo-a:2", "--bit-Foo-b:3", "padding:2px"));
        Assert.AreEqual("padding:2px", variables.Prepend(null, null, "padding:2px"));
    }
}
