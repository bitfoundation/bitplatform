using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Contract and behavior for the re-substitution of the tokens derived from the spacing unit: an inline theme
/// override (a <see cref="BitThemeProvider"/> wrapper or <c>BitThemeManager.ApplyBitThemeAsync</c>, which share the
/// same augmentation) that re-values only the density scale or the spacing unit re-declares the dialog and card
/// insets and the density-aware sizes on the same element, as the very expression every packaged preset declares them
/// with, so the subtree resizes against the active preset's steps instead of inheriting the document's
/// already-computed length - while a value fixed above, by a preset or by a theme, is kept.
/// </summary>
[TestClass]
public sealed class BitThemeSpacingReSubstitutionTests : BunitTestContext
{
    /// <summary>One row of <c>BitThemeMapper.SpacingDerivedTargets</c>, read as the mapper reads it.</summary>
    private sealed record Target(string Token, string Steps, string Fixed, string Expression, string ReSubstitution);

    /// <summary>One declaration of a custom property in a theme stylesheet, with the rule it is written in.</summary>
    private sealed record Declaration(string File, int Rule, string Name, string Value);

    private static readonly Regex InnermostRule = new(@"\{([^{}]*)\}", RegexOptions.Compiled);

    private static readonly Regex CustomPropertyDeclaration = new(@"(--bit-[a-z0-9-]+)\s*:\s*([^;]+);", RegexOptions.Compiled);

    // A CSS <number>: an optional sign, then digits with an optional fraction, or a fraction alone (.75).
    private static readonly Regex UnitlessNumber = new(@"^[+-]?(?:\d+(?:\.\d+)?|\.\d+)$", RegexOptions.Compiled);

    private static readonly Lazy<IReadOnlyList<Target>> LazyTargets = new(ReadTargets);

    private static readonly Lazy<IReadOnlyList<Declaration>> LazyDeclarations = new(ReadDeclarations);

    /// <summary>
    /// The mapper's table - read off the compiled mapper rather than restated here, so a steps or fixed name renamed
    /// or mistyped there is one these tests look for in the stylesheets.
    /// </summary>
    private static IReadOnlyList<Target> Targets => LazyTargets.Value;

    /// <summary>Every custom property declared in a theme stylesheet - the core ones and the Extras presets alike.</summary>
    private static IReadOnlyList<Declaration> Declarations => LazyDeclarations.Value;

    private static Target TargetFor(string token) => Targets.Single(target => target.Token == token);

    private static IReadOnlyList<Target> ReadTargets()
    {
        const string mapperName = "Bit.BlazorUI.BitThemeMapper";
        const string tableName = "SpacingDerivedTargets";

        var mapper = typeof(BitThemeProvider).Assembly.GetType(mapperName);
        Assert.IsNotNull(mapper, $"{mapperName} was not found; these tests read its {tableName} table.");

        var table = mapper.GetField(tableName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(table, $"{mapperName}.{tableName} was not found as a static field; these tests read it.");

        var rows = table.GetValue(null) as System.Collections.IEnumerable;
        Assert.IsNotNull(rows, $"{mapperName}.{tableName} is not a list.");

        string Read(object row, string property)
        {
            var value = row.GetType().GetProperty(property)?.GetValue(row) as string;
            Assert.IsNotNull(value, $"A row of {mapperName}.{tableName} has no string {property}.");
            return value;
        }

        return rows.Cast<object>()
                   .Select(row => new Target(Read(row, "Token"), Read(row, "Steps"), Read(row, "Fixed"), Read(row, "Expression"), Read(row, "ReSubstitution")))
                   .ToArray();
    }

    private static IReadOnlyList<Declaration> ReadDeclarations()
    {
        return SourceFiles.EnumerateThemeStylesheets()
                          .SelectMany(file =>
                          {
                              var name = Path.GetRelativePath(SourceFiles.Root, file);
                              var scss = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file));

                              return InnermostRule.Matches(scss)
                                                  .SelectMany(rule => CustomPropertyDeclaration.Matches(rule.Groups[1].Value)
                                                                                               .Select(d => new Declaration(name, rule.Index, d.Groups[1].Value, d.Groups[2].Value.Trim())));
                          })
                          .ToArray();
    }

    private static bool IsCompanion(string name) => name.EndsWith("-steps", StringComparison.Ordinal) || name.EndsWith("-fixed", StringComparison.Ordinal);

    private static bool IsDerivedFromTheSpacingUnit(string value)
        => value.Contains("--bit-spa-scaling-factor", StringComparison.Ordinal) ||
           value.Contains("--bit-layout-density-scale", StringComparison.Ordinal) ||
           value.Contains("spacing(", StringComparison.Ordinal);

    private static IEnumerable<Declaration> InTheSameRule(Declaration declaration, string name)
        => Declarations.Where(d => d.File == declaration.File && d.Rule == declaration.Rule && d.Name == name);

    private string RenderProviderStyle(BitTheme theme)
    {
        var cut = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Theme, theme);
            parameters.AddChildContent("<span>content</span>");
        });

        return cut.Find("div").GetAttribute("style") ?? string.Empty;
    }

    [TestMethod]
    public void TheMapperReSubstitutesEveryTokenDerivedFromTheSpacingUnit()
    {
        var derivedInStylesheets = Declarations.Where(d => IsCompanion(d.Name) is false && IsDerivedFromTheSpacingUnit(d.Value))
                                               .Select(d => d.Name)
                                               .Where(name => name != "--bit-spa-scaling-factor" && name != "--bit-layout-density-scale")
                                               .ToHashSet(StringComparer.Ordinal);

        CollectionAssert.Contains(derivedInStylesheets.ToArray(), "--bit-spa-dialog");
        CollectionAssert.Contains(derivedInStylesheets.ToArray(), "--bit-siz-ctrl-md");

        var inTheTable = Targets.Select(target => target.Token).ToHashSet(StringComparer.Ordinal);

        var missing = derivedInStylesheets.Except(inTheTable).Order(StringComparer.Ordinal).ToArray();
        var stale = inTheTable.Except(derivedInStylesheets).Order(StringComparer.Ordinal).ToArray();

        Assert.AreEqual(0, missing.Length,
            $"A theme stylesheet derives {string.Join(", ", missing)} from the spacing unit, but BitThemeMapper.SpacingDerivedTargets has no row for it, so a scoped density override leaves it at the document's length.");
        Assert.AreEqual(0, stale.Length,
            $"BitThemeMapper.SpacingDerivedTargets has a row for {string.Join(", ", stale)}, which no theme stylesheet derives from the spacing unit.");
    }

    [TestMethod]
    public void EveryDeclarationOfADerivedTokenIsOneTheMapperCanReproduce()
    {
        var checkedDeclarations = 0;

        foreach (var target in Targets)
        {
            foreach (var declaration in Declarations.Where(d => d.Name == target.Token))
            {
                checkedDeclarations++;

                if (declaration.Value == target.Expression)
                {
                    var steps = InTheSameRule(declaration, target.Steps).ToArray();

                    Assert.AreEqual(1, steps.Length,
                        $"{declaration.File} derives {target.Token} from {target.Steps} without declaring it in the same rule.");
                    Assert.IsTrue(UnitlessNumber.IsMatch(steps[0].Value),
                        $"{declaration.File} declares {target.Steps} as '{steps[0].Value}'; the steps of the spacing unit are a unitless number.");
                }
                else if (declaration.Value == $"var({target.Fixed})")
                {
                    var fixedValue = InTheSameRule(declaration, target.Fixed).ToArray();

                    Assert.AreEqual(1, fixedValue.Length,
                        $"{declaration.File} reads {target.Token} from {target.Fixed} without declaring it in the same rule.");
                    Assert.AreNotEqual("initial", fixedValue[0].Value,
                        $"{declaration.File} reads {target.Token} from {target.Fixed}, which it declares initial.");
                }
                else
                {
                    Assert.Fail($"{declaration.File} declares {target.Token} as '{declaration.Value}'. A scoped density override re-declares it as '{target.ReSubstitution}', " +
                                $"so it is either '{target.Expression}' beside a unitless {target.Steps}, or 'var({target.Fixed})' beside the value {target.Fixed} declares.");
                }
            }
        }

        Assert.IsTrue(checkedDeclarations >= Targets.Count, "Expected every derived token to be declared by at least the core Fluent stylesheets.");
    }

    [TestMethod]
    public void TheCoreFluentStylesheetsDeclareEveryDerivedToken()
    {
        var core = Declarations.Where(d => d.File.StartsWith(Path.Combine("Bit.BlazorUI", "Styles", "Fluent"), StringComparison.Ordinal))
                               .Select(d => d.Name)
                               .ToHashSet(StringComparer.Ordinal);

        var missing = Targets.Select(target => target.Token).Where(token => core.Contains(token) is false).ToArray();

        Assert.AreEqual(0, missing.Length,
            $"The core Fluent stylesheets, which every preset is layered over, do not declare {string.Join(", ", missing)}.");
    }

    [TestMethod]
    public void APresetThatDerivesATokenAnotherOneFixesClearsTheFixedValue()
    {
        foreach (var target in Targets)
        {
            var fixedSomewhere = Declarations.Any(d => d.Name == target.Fixed && d.Value != "initial");

            if (fixedSomewhere is false) continue;

            foreach (var derived in Declarations.Where(d => d.Name == target.Token && d.Value == target.Expression))
            {
                Assert.IsTrue(InTheSameRule(derived, target.Fixed).Any(d => d.Value == "initial"),
                    $"{derived.File} derives {target.Token}, which another preset fixes, without declaring {target.Fixed}: initial - a scoped density override in a subtree scoped to it would land on the other preset's fixed value.");
            }
        }
    }

    [TestMethod]
    public void DensityOnlyOverrideReDeclaresTheDerivedTokens()
    {
        var style = RenderProviderStyle(new BitTheme { Layout = { DensityScale = "0.9" } });

        foreach (var target in Targets)
        {
            StringAssert.Contains(style, $"{target.Token}:{target.ReSubstitution}");
        }
    }

    [TestMethod]
    public void ScalingFactorOnlyOverrideReDeclaresTheDerivedTokens()
    {
        var style = RenderProviderStyle(new BitTheme { Spacing = { ScalingFactor = "0.25rem" } });

        foreach (var target in Targets)
        {
            StringAssert.Contains(style, $"{target.Token}:{target.ReSubstitution}");
        }
    }

    [TestMethod]
    public void TheReSubstitutionPrefersAFixedValueOverTheSteps()
    {
        var target = TargetFor("--bit-spa-dialog");

        Assert.AreEqual("--bit-spa-dialog-steps", target.Steps);
        Assert.AreEqual("--bit-spa-dialog-fixed", target.Fixed);
        Assert.AreEqual("calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale) * var(--bit-spa-dialog-steps))", target.Expression);
        Assert.AreEqual($"var(--bit-spa-dialog-fixed, {target.Expression})", target.ReSubstitution);
    }

    [TestMethod]
    public void ExplicitCardInsetWins()
    {
        var theme = new BitTheme { Layout = { DensityScale = "0.9" } };
        theme.Spacing.Card.Md = "10px";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-spa-card-md:10px");
        Assert.IsFalse(style.Contains($"--bit-spa-card-md:{TargetFor("--bit-spa-card-md").ReSubstitution}", StringComparison.Ordinal));
        StringAssert.Contains(style, $"--bit-spa-card-sm:{TargetFor("--bit-spa-card-sm").ReSubstitution}");
        StringAssert.Contains(style, $"--bit-spa-card-lg:{TargetFor("--bit-spa-card-lg").ReSubstitution}");
        StringAssert.Contains(style, $"--bit-spa-dialog:{TargetFor("--bit-spa-dialog").ReSubstitution}");
    }

    [TestMethod]
    public void ExplicitDialogInsetWins()
    {
        var theme = new BitTheme { Layout = { DensityScale = "0.9" } };
        theme.Spacing.Dialog = "20px";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-spa-dialog:20px");
        Assert.IsFalse(style.Contains($"--bit-spa-dialog:{TargetFor("--bit-spa-dialog").ReSubstitution}", StringComparison.Ordinal));
        StringAssert.Contains(style, $"--bit-spa-card-md:{TargetFor("--bit-spa-card-md").ReSubstitution}");
    }

    [TestMethod]
    public void ExplicitControlSizeWins()
    {
        var theme = new BitTheme { Layout = { DensityScale = "0.9" } };
        theme.Size.Control.Md = "40px";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-siz-ctrl-md:40px");
        Assert.IsFalse(style.Contains($"--bit-siz-ctrl-md:{TargetFor("--bit-siz-ctrl-md").ReSubstitution}", StringComparison.Ordinal));
        StringAssert.Contains(style, $"--bit-siz-ctrl-sm:{TargetFor("--bit-siz-ctrl-sm").ReSubstitution}");
    }

    [TestMethod]
    public void AnExplicitValueIsFixedForTheRegionsNestedInside()
    {
        var theme = new BitTheme();
        theme.Spacing.Dialog = "32px";
        theme.Size.Control.Md = "40px";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-spa-dialog-fixed:var(--bit-spa-dialog)");
        StringAssert.Contains(style, "--bit-siz-ctrl-md-fixed:var(--bit-siz-ctrl-md)");
        Assert.IsFalse(style.Contains("--bit-spa-card-md", StringComparison.Ordinal),
            $"Neither input changed, so nothing but the explicit values is fixed or re-declared. Actual: {style}");
    }

    [TestMethod]
    public void ANestedProviderKeepsTheValueItsParentSetExplicitly()
    {
        var parent = new BitTheme();
        parent.Spacing.Dialog = "32px";

        var cut = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Theme, parent);
            parameters.AddChildContent<BitThemeProvider>(child =>
            {
                child.Add(p => p.Theme, new BitTheme { Layout = { DensityScale = "0.9" } });
                child.AddChildContent("<span>content</span>");
            });
        });

        var inner = cut.FindAll("div")[1].GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(inner, "--bit-spa-dialog:32px");
        StringAssert.Contains(inner, $"--bit-spa-card-md:{TargetFor("--bit-spa-card-md").ReSubstitution}");
    }

    [TestMethod]
    public void UntouchedDensityAndSpacingUnitLeaveTheDerivedTokensInherited()
    {
        var style = RenderProviderStyle(new BitTheme { Shape = { BorderRadius = "4px" } });

        foreach (var target in Targets)
        {
            Assert.IsFalse(style.Contains(target.Token, StringComparison.Ordinal),
                $"{target.Token} must not be re-declared when neither input changed. Actual: {style}");
        }
    }
}
