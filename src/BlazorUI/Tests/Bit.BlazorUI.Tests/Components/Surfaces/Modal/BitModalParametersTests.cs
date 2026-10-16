using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

/// <summary>
/// Covers BitModalParameters, the set a Modal is shown with through the service and cascaded to it by its container:
/// how two sets merge (the first wins, the callbacks compose, the attributes union), that the merge carries every
/// member, and that it names every parameter of the component. The BitParams cascade (BitModalParams) is a different
/// thing, covered by BitModalParamsTests.
/// </summary>
[TestClass]
public class BitModalParametersTests
{
    // The members the merge handles on their own terms, each pinned by a test of its own below.
    private static readonly string[] _mergedApart =
    [
        nameof(BitModalParameters.HtmlAttributes),
        nameof(BitModalParameters.Classes),
        nameof(BitModalParameters.Styles),
        nameof(BitModalParameters.OnDismiss),
        nameof(BitModalParameters.OnEscapeKeyDown),
        nameof(BitModalParameters.OnOpen),
        nameof(BitModalParameters.OnOverlayClick),
    ];

    [TestMethod]
    public void MergeShouldCarryEveryMemberAcrossWithTheFirstWinning()
    {
        // A member added to the parameters but forgotten in Merge is one a container-level default never reaches a
        // modal through, and one a modal's own showing never gets to override.
        var properties = typeof(BitModalParameters).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                   .Where(p => p.CanWrite && _mergedApart.Contains(p.Name) is false)
                                                   .ToArray();

        Assert.IsTrue(properties.Length > 30);

        var second = new BitModalParameters();
        foreach (var property in properties)
        {
            property.SetValue(second, SampleValue(property.PropertyType, 1));
        }

        var fallback = BitModalParameters.Merge(new BitModalParameters(), second)!;
        foreach (var property in properties)
        {
            Assert.AreEqual(property.GetValue(second), property.GetValue(fallback), $"{property.Name} does not fall back to the second set.");
        }

        var first = new BitModalParameters();
        foreach (var property in properties)
        {
            property.SetValue(first, SampleValue(property.PropertyType, 0));
        }

        var merged = BitModalParameters.Merge(first, second)!;
        foreach (var property in properties)
        {
            Assert.AreEqual(property.GetValue(first), property.GetValue(merged), $"{property.Name} of the first set does not win.");
        }

        // Two distinct values per type, so a member the merge drops or picks from the wrong side is told apart.
        static object SampleValue(System.Type type, int which)
        {
            var underlying = System.Nullable.GetUnderlyingType(type) ?? type;

            if (underlying == typeof(bool)) return which == 0;
            if (underlying == typeof(string)) return which == 0 ? "first" : "second";
            if (underlying == typeof(BitIconInfo)) return which == 0 ? BitIconInfo.Css("first") : BitIconInfo.Css("second");
            if (underlying == typeof(ElementReference)) return new ElementReference(which == 0 ? "first" : "second");
            if (underlying == typeof(RenderFragment)) return which == 0 ? (RenderFragment)(b => b.AddContent(0, "first")) : (RenderFragment)(b => b.AddContent(0, "second"));
            if (underlying == typeof(System.Func<System.Threading.Tasks.Task<bool>>)) return which == 0 ? (System.Func<System.Threading.Tasks.Task<bool>>)(() => System.Threading.Tasks.Task.FromResult(true)) : (System.Func<System.Threading.Tasks.Task<bool>>)(() => System.Threading.Tasks.Task.FromResult(false));
            if (underlying.IsEnum)
            {
                var values = System.Enum.GetValues(underlying);
                return values.GetValue(which == 0 ? 0 : values.Length - 1)!;
            }

            throw new System.NotSupportedException($"Add a sample value for {type}.");
        }
    }

    [TestMethod]
    public async System.Threading.Tasks.Task MergeShouldComposeTheEscapeCallbacksInvokingFirstThenSecond()
    {
        var order = new List<string>();

        var first = new BitModalParameters
        {
            OnEscapeKeyDown = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.KeyboardEventArgs>(new object(), () => order.Add("first")),
        };
        var second = new BitModalParameters
        {
            OnEscapeKeyDown = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.KeyboardEventArgs>(new object(), () => order.Add("second")),
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.IsTrue(merged.OnEscapeKeyDown.HasDelegate);
        Assert.IsFalse(BitModalParameters.Merge(new BitModalParameters(), new BitModalParameters())!.OnEscapeKeyDown.HasDelegate);

        await merged.OnEscapeKeyDown.InvokeAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs());

        CollectionAssert.AreEqual(new[] { "first", "second" }, order);
    }

    [TestMethod]
    public void MergeShouldKeepACallbackOfOneSideAlone()
    {
        var calls = 0;

        var only = new BitModalParameters
        {
            OnOpen = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(new object(), () => calls++),
        };

        var merged = BitModalParameters.Merge(new BitModalParameters(), only)!;

        Assert.IsTrue(merged.OnOpen.HasDelegate);

        merged.OnOpen.InvokeAsync().GetAwaiter().GetResult();

        Assert.AreEqual(1, calls);
    }
    [TestMethod]
    public void MergeShouldReturnNullWhenBothNull()
    {
        Assert.IsNull(BitModalParameters.Merge(null, null));
    }

    [TestMethod]
    public void MergeShouldReturnTheOtherWhenOneIsNull()
    {
        var p = new BitModalParameters();

        Assert.AreSame(p, BitModalParameters.Merge(p, null));
        Assert.AreSame(p, BitModalParameters.Merge(null, p));
    }

    [TestMethod]
    public void MergeShouldGivePrecedenceToFirstParameters()
    {
        var first = new BitModalParameters { Blocking = true, FullWidth = true };
        var second = new BitModalParameters { Blocking = false, FullHeight = true };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual(true, merged.Blocking);   // first wins
        Assert.AreEqual(true, merged.FullWidth);  // only set on first
        Assert.AreEqual(true, merged.FullHeight); // only set on second
    }

    [TestMethod]
    public void MergeShouldCarryEverySizeAcrossWithTheFirstWinning()
    {
        var first = new BitModalParameters { Width = "20rem", MaxWidth = "28rem" };
        var second = new BitModalParameters { Width = "30rem", Height = "12rem", MaxHeight = "16rem" };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual("20rem", merged.Width);      // first wins
        Assert.AreEqual("28rem", merged.MaxWidth);   // only set on first
        Assert.AreEqual("12rem", merged.Height);     // only set on second
        Assert.AreEqual("16rem", merged.MaxHeight);  // only set on second
    }

    [TestMethod]
    public void MergeShouldFallBackToSecondForUnsetNullableValues()
    {
        var first = new BitModalParameters();
        var second = new BitModalParameters { NoScrollLock = true, AriaModal = false };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual(true, merged.NoScrollLock);
        Assert.AreEqual(false, merged.AriaModal);
    }

    [TestMethod]
    public void MergeShouldUnionHtmlAttributesWithFirstWinningOnConflicts()
    {
        var first = new BitModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-test"] = "first", ["data-a"] = "a" }
        };
        var second = new BitModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-test"] = "second", ["data-b"] = "b" }
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual("first", merged.HtmlAttributes["data-test"]);
        Assert.AreEqual("a", merged.HtmlAttributes["data-a"]);
        Assert.AreEqual("b", merged.HtmlAttributes["data-b"]);
    }

    [TestMethod]
    public void MergeShouldHandleNullHtmlAttributes()
    {
        var firstNull = new BitModalParameters { HtmlAttributes = null! };
        var second = new BitModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-b"] = "b" }
        };

        var mergedFirstNull = BitModalParameters.Merge(firstNull, second)!;
        Assert.IsNotNull(mergedFirstNull.HtmlAttributes);
        Assert.AreEqual("b", mergedFirstNull.HtmlAttributes["data-b"]);

        var first = new BitModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-a"] = "a" }
        };
        var secondNull = new BitModalParameters { HtmlAttributes = null! };

        var mergedSecondNull = BitModalParameters.Merge(first, secondNull)!;
        Assert.IsNotNull(mergedSecondNull.HtmlAttributes);
        Assert.AreEqual("a", mergedSecondNull.HtmlAttributes["data-a"]);

        var bothNull = BitModalParameters.Merge(
            new BitModalParameters { HtmlAttributes = null! },
            new BitModalParameters { HtmlAttributes = null! })!;
        Assert.IsNotNull(bothNull.HtmlAttributes);
        Assert.AreEqual(0, bothNull.HtmlAttributes.Count);
    }

    [TestMethod]
    public void MergeShouldFallBackToSecondForTheUnsetDialogBehaviorFlags()
    {
        var first = new BitModalParameters { NoAutoFocus = true };
        var second = new BitModalParameters
        {
            NoAutoFocus = false,
            NoDismissOnEscape = true,
            NoFocusTrap = true,
            NoRestoreFocus = true,
            AriaLabel = "Cascaded label",
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual(true, merged.NoAutoFocus);        // first wins
        Assert.AreEqual(true, merged.NoDismissOnEscape);  // only set on second
        Assert.AreEqual(true, merged.NoFocusTrap);        // only set on second
        Assert.AreEqual(true, merged.NoRestoreFocus);     // only set on second
        Assert.AreEqual("Cascaded label", merged.AriaLabel);
    }

    [TestMethod]
    public void MergeShouldKeepAnUnsetFlagUnset()
    {
        // null is "not set", which is what lets the BitModal default (or the value it was given on the
        // component itself) stand rather than being overridden with a false nobody asked for.
        var merged = BitModalParameters.Merge(new BitModalParameters(), new BitModalParameters())!;

        Assert.IsNull(merged.NoAutoFocus);
        Assert.IsNull(merged.NoDismissOnEscape);
        Assert.IsNull(merged.NoFocusTrap);
        Assert.IsNull(merged.NoRestoreFocus);
        Assert.IsNull(merged.AriaModal);
        Assert.IsNull(merged.Modeless);
        Assert.IsNull(merged.AriaLabel);
    }

    [TestMethod]
    public void MergeShouldReportNoDelegateWhenNeitherSourceHasOne()
    {
        var merged = BitModalParameters.Merge(new BitModalParameters(), new BitModalParameters())!;

        Assert.IsFalse(merged.OnDismiss.HasDelegate);
        Assert.IsFalse(merged.OnOpen.HasDelegate);
        Assert.IsFalse(merged.OnOverlayClick.HasDelegate);
    }

    [TestMethod]
    public async System.Threading.Tasks.Task MergeShouldComposeTheOnOpenCallbacksInvokingFirstThenSecond()
    {
        var order = new List<string>();

        var first = new BitModalParameters
        {
            OnOpen = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(new object(), () => order.Add("open-first")),
        };
        var second = new BitModalParameters
        {
            OnOpen = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(new object(), () => order.Add("open-second")),
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.IsTrue(merged.OnOpen.HasDelegate);

        await merged.OnOpen.InvokeAsync();

        CollectionAssert.AreEqual(new[] { "open-first", "open-second" }, order);
    }

    [TestMethod]
    public void MergeShouldCarryTheRootClassAndStyleAcrossWithTheFirstWinning()
    {
        var first = new BitModalParameters { Style = "--bit-Modal-radius:1rem" };
        var second = new BitModalParameters { Class = "house", Style = "--bit-Modal-radius:0" };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual("house", merged.Class);
        Assert.AreEqual("--bit-Modal-radius:1rem", merged.Style);
    }

    [TestMethod]
    public void BitModalParametersShouldCarryEveryParameterOfTheComponent()
    {
        // A parameter added to the Modal without its counterpart here is one a Modal shown through the service can
        // never be given. Only the open state and the content are the service's own business.
        var carried = typeof(BitModalParameters).GetProperties().Select(p => p.Name).ToHashSet();
        carried.UnionWith(
        [
            nameof(BitModal.IsOpen),
            nameof(BitModal.IsOpenChanged),
            nameof(BitModal.DefaultIsOpen),
            nameof(BitModal.ChildContent),
        ]);

        var missing = typeof(BitModal).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                      .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                      .Select(p => p.Name)
                                      .Where(n => carried.Contains(n) is false)
                                      .ToList();

        // The base parameters that shape how one Modal looks or reads are carried too.
        missing.AddRange(new[]
        {
            nameof(BitModal.AriaLabel), nameof(BitModal.Class), nameof(BitModal.Dir), nameof(BitModal.HtmlAttributes),
            nameof(BitModal.Disabled), nameof(BitModal.Style), nameof(BitModal.Visibility)
        }.Where(n => carried.Contains(n) is false));

        CollectionAssert.AreEqual(new List<string>(), missing, string.Join(", ", missing));
    }

    [TestMethod]
    public void MergeShouldMergeTheClassesAndStylesOfEachPart()
    {
        var first = new BitModalParameters
        {
            Classes = new BitModalClassStyles { Root = "first-root" },
            Styles = new BitModalClassStyles { Overlay = "color:red" },
        };
        var second = new BitModalParameters
        {
            Classes = new BitModalClassStyles { Root = "second-root", Content = "second-content" },
            Styles = new BitModalClassStyles { Overlay = "color:green", Content = "color:blue" },
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual("first-root", merged.Classes!.Root);
        Assert.AreEqual("second-content", merged.Classes!.Content);
        Assert.AreEqual("color:red", merged.Styles!.Overlay);
        Assert.AreEqual("color:blue", merged.Styles!.Content);
    }

    [TestMethod]
    public async System.Threading.Tasks.Task MergeShouldComposeCallbacksInvokingFirstThenSecond()
    {
        var order = new List<string>();

        var first = new BitModalParameters
        {
            OnDismiss = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("dismiss-first")),
            OnOverlayClick = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("overlay-first")),
        };
        var second = new BitModalParameters
        {
            OnDismiss = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("dismiss-second")),
            OnOverlayClick = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("overlay-second")),
        };

        var merged = BitModalParameters.Merge(first, second)!;

        await merged.OnDismiss.InvokeAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await merged.OnOverlayClick.InvokeAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        CollectionAssert.AreEqual(
            new[] { "dismiss-first", "dismiss-second", "overlay-first", "overlay-second" },
            order);
    }

    [TestMethod]
    public void MergeShouldCarryTheChromeAcrossWithTheFirstWinning()
    {
        var first = new BitModalParameters
        {
            HeaderText = "first",
            ShowCloseButton = true,
            CloseButtonTitle = "first-close",
            Visibility = BitVisibility.Hidden,
        };
        var second = new BitModalParameters
        {
            HeaderText = "second",
            FooterText = "second-footer",
            CloseIconName = "Cancel",
            Visibility = BitVisibility.Collapsed,
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual("first", merged.HeaderText);           // first wins
        Assert.AreEqual("first-close", merged.CloseButtonTitle); // only set on first
        Assert.AreEqual(true, merged.ShowCloseButton);           // only set on first
        Assert.AreEqual("second-footer", merged.FooterText);     // only set on second
        Assert.AreEqual("Cancel", merged.CloseIconName);         // only set on second
        Assert.AreEqual(BitVisibility.Hidden, merged.Visibility);
    }

    [TestMethod]
    public void MergeShouldCarryTheLayoutAndScrollHandlingAcrossWithTheFirstWinning()
    {
        var first = new BitModalParameters
        {
            Position = BitPosition.TopRight,
            AbsolutePosition = true,
            Draggable = true,
        };
        var second = new BitModalParameters
        {
            Position = BitPosition.BottomLeft,
            ModeFull = true,
            Modeless = true,
            NoBorder = true,
            FullSize = true,
            AutoToggleScroll = true,
            DragElementSelector = "#handle",
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual(BitPosition.TopRight, merged.Position); // first wins
        Assert.AreEqual(true, merged.AbsolutePosition);
        Assert.AreEqual(true, merged.Draggable);
        Assert.AreEqual(true, merged.ModeFull);
        Assert.AreEqual(true, merged.Modeless);
        Assert.AreEqual(true, merged.NoBorder);
        Assert.AreEqual(true, merged.FullSize);
        Assert.AreEqual(true, merged.AutoToggleScroll);
        Assert.AreEqual("#handle", merged.DragElementSelector);
    }

    [TestMethod]
    public void MergeShouldMergeTheClassesAndStylesOfEveryChromePart()
    {
        var first = new BitModalParameters
        {
            Classes = new BitModalClassStyles { HeaderContainer = "first-hcn", Body = "first-bdy" },
            Styles = new BitModalClassStyles { Header = "color:red" },
        };
        var second = new BitModalParameters
        {
            Classes = new BitModalClassStyles { HeaderContainer = "second-hcn", Footer = "second-fcn", CloseIcon = "second-cic" },
            Styles = new BitModalClassStyles { Header = "color:green", CloseButton = "color:blue" },
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual("first-hcn", merged.Classes!.HeaderContainer); // first wins
        Assert.AreEqual("first-bdy", merged.Classes.Body);             // only set on first
        Assert.AreEqual("second-fcn", merged.Classes.Footer);          // only set on second
        Assert.AreEqual("second-cic", merged.Classes.CloseIcon);       // only set on second
        Assert.AreEqual("color:red", merged.Styles!.Header);           // first wins
        Assert.AreEqual("color:blue", merged.Styles.CloseButton);      // only set on second
    }
}
