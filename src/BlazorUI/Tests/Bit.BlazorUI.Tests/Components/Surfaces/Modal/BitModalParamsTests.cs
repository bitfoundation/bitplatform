using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

[TestClass]
public class BitModalParamsTests : BunitTestContext
{
    [TestMethod]
    public void BitModalShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitModalCascadingParamsTest>();

        var modals = component.FindComponents<BitModal>();

        Assert.AreEqual(2, modals.Count);

        // The first Modal takes everything from the cascading parameters.
        var first = modals[0].Find(".bit-mdl");
        foreach (var cls in new[] { "bit-mdl-tcr", "bit-mdl-mfl", "cascaded-root" })
        {
            Assert.IsTrue(first.ClassList.Contains(cls), cls);
        }
        Assert.IsFalse(first.ClassList.Contains("bit-mdl-bdr"));

        var firstContent = modals[0].Find(".bit-mdl-ctn");
        Assert.IsTrue(firstContent.ClassList.Contains("cascaded-content"));
        StringAssert.Contains(firstContent.GetAttribute("style"), "max-width:30rem;");
        StringAssert.Contains(firstContent.GetAttribute("style"), "margin: 1px;");
        Assert.AreEqual("alertdialog", firstContent.GetAttribute("role"));
        Assert.AreEqual("Dismiss", modals[0].Find(".bit-mdl-cls").GetAttribute("aria-label"));

        // The second one sets its own position, width and close title, which the cascade must not overwrite.
        var second = modals[1].Find(".bit-mdl");
        Assert.IsTrue(second.ClassList.Contains("bit-mdl-bst"));
        Assert.IsFalse(second.ClassList.Contains("bit-mdl-tcr"));
        Assert.IsTrue(second.ClassList.Contains("second"));
        Assert.IsTrue(second.ClassList.Contains("bit-mdl-mfl"));
        StringAssert.Contains(modals[1].Find(".bit-mdl-ctn").GetAttribute("style"), "max-width:10rem;");
        Assert.AreEqual("Close it", modals[1].Find(".bit-mdl-cls").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitModalShouldTakeCascadedBehaviorParams()
    {
        var isOpen = true;

        var component = RenderComponent<CascadingValue<BitModalParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitModalParams.ParamName);
            parameters.Add(p => p.Value, new BitModalParams { Blocking = true, IsAlert = false });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitModal>(0);
                builder.AddComponentParameter(1, nameof(BitModal.IsOpen), isOpen);
                builder.AddComponentParameter(2, nameof(BitModal.IsOpenChanged), EventCallback.Factory.Create<bool>(this, v => isOpen = v));
                builder.CloseComponent();
            }));
        });

        component.Find(".bit-mdl-ovl").Click();

        // A cascaded Blocking keeps the Modal open on an overlay click, and a cascaded IsAlert = false keeps the plain
        // dialog role a blocking Modal would otherwise swap for alertdialog.
        Assert.IsTrue(isOpen);
        Assert.AreEqual("dialog", component.Find(".bit-mdl-ctn").GetAttribute("role"));
    }

    [TestMethod]
    public async Task BitModalServiceParametersShouldWinOverTheCascadedDefaults()
    {
        Services.AddSingleton<BitModalService>();

        var component = RenderComponent<CascadingValue<BitModalParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitModalParams.ParamName);
            parameters.Add(p => p.Value, new BitModalParams { MaxWidth = "30rem", Position = BitPosition.TopCenter, ModeFull = true, Blocking = true });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitModalContainer>(0);
                builder.CloseComponent();
            }));
        });

        var modalService = Services.GetRequiredService<BitModalService>();

        await modalService.Show(builder => builder.AddContent(0, "shown"), new BitModalParameters
        {
            MaxWidth = "20rem",
            Position = BitPosition.BottomEnd,
            Blocking = false,
        });

        component.WaitForAssertion(() =>
        {
            // What one showing asks for beats the app-wide default, and what it leaves out falls back to it.
            var root = component.Find(".bit-mdl");
            Assert.IsTrue(root.ClassList.Contains("bit-mdl-ben"));
            Assert.IsFalse(root.ClassList.Contains("bit-mdl-tcr"));
            Assert.IsTrue(root.ClassList.Contains("bit-mdl-mfl"));
            StringAssert.Contains(component.Find(".bit-mdl-ctn").GetAttribute("style"), "max-width:20rem;");
        });

        // The show turned Blocking back off, so the overlay dismisses the Modal despite the cascaded default.
        component.Find(".bit-mdl-ovl").Click();

        component.WaitForAssertion(() => Assert.AreEqual(0, component.FindAll(".bit-mdl").Count));
    }

    [TestMethod]
    public async Task BitModalContainerParametersShouldWinOverTheCascadedDefaults()
    {
        Services.AddSingleton<BitModalService>();

        var component = RenderComponent<CascadingValue<BitModalParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitModalParams.ParamName);
            parameters.Add(p => p.Value, new BitModalParams { Position = BitPosition.TopCenter, ShowCloseButton = true, Style = "--bit-Modal-radius:0" });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitModalContainer>(0);
                builder.AddComponentParameter(1, nameof(BitModalContainer.ModalParameters), new BitModalParameters { Position = BitPosition.BottomCenter });
                builder.CloseComponent();
            }));
        });

        var modalService = Services.GetRequiredService<BitModalService>();

        await modalService.Show(builder => builder.AddContent(0, "shown"));

        // The container's own parameters are the defaults of its modals, so they beat the app-wide ones; what the
        // container leaves out still falls back to the BitParams ancestor.
        component.WaitForAssertion(() =>
        {
            var root = component.Find(".bit-mdl");
            Assert.IsTrue(root.ClassList.Contains("bit-mdl-bcr"));
            Assert.IsFalse(root.ClassList.Contains("bit-mdl-tcr"));
            StringAssert.Contains(root.GetAttribute("style"), "--bit-Modal-radius:0");
            Assert.AreEqual(1, component.FindAll(".bit-mdl-cls").Count);
        });
    }

    [TestMethod]
    public async Task BitModalServiceParametersShouldWinOverTheCascadedBaseDefaults()
    {
        Services.AddSingleton<BitModalService>();

        var component = RenderComponent<CascadingValue<BitModalParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitModalParams.ParamName);
            parameters.Add(p => p.Value, new BitModalParams { Dir = BitDir.Rtl, AriaLabel = "App-wide name", IsEnabled = false });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitModalContainer>(0);
                builder.CloseComponent();
            }));
        });

        var modalService = Services.GetRequiredService<BitModalService>();

        await modalService.Show(builder => builder.AddContent(0, "shown"), new BitModalParameters
        {
            Dir = BitDir.Ltr,
            AriaLabel = "Shown name",
            IsEnabled = true,
        });

        // The parameters declared on the base component follow the same precedence as the Modal's own: what one
        // showing asks for beats the app-wide default a BitParams ancestor put on the Modal.
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("ltr", component.Find(".bit-mdl").GetAttribute("dir"));
            Assert.AreEqual("Shown name", component.Find(".bit-mdl-ctn").GetAttribute("aria-label"));
        });

        component.Find(".bit-mdl-ovl").Click();

        component.WaitForAssertion(() => Assert.AreEqual(0, component.FindAll(".bit-mdl").Count));
    }

    [TestMethod]
    public async Task BitModalShouldMarkItsRootOffTheEffectiveBaseParameters()
    {
        Services.AddSingleton<BitModalService>();

        var component = RenderComponent<BitModalContainer>();

        var modalService = Services.GetRequiredService<BitModalService>();

        await modalService.Show(builder => builder.AddContent(0, "shown"), new BitModalParameters
        {
            Dir = BitDir.Rtl,
            IsEnabled = false,
        });

        // The markers the base component puts on the root off its own parameters are put there for the values a
        // showing asks for as well.
        component.WaitForAssertion(() =>
        {
            var root = component.Find(".bit-mdl");
            Assert.IsTrue(root.ClassList.Contains("bit-dis"));
            Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
            Assert.AreEqual(1, root.ClassList.Count(c => c == "bit-dis"));
        });
    }

    [TestMethod]
    public async Task BitModalShouldNotKeepTheMarksOfACascadedBaseDefaultItsShowingOverrode()
    {
        Services.AddSingleton<BitModalService>();

        var component = RenderComponent<CascadingValue<BitModalParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitModalParams.ParamName);
            parameters.Add(p => p.Value, new BitModalParams { Dir = BitDir.Rtl, IsEnabled = false, Visibility = BitVisibility.Hidden });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitModalContainer>(0);
                builder.CloseComponent();
            }));
        });

        var modalService = Services.GetRequiredService<BitModalService>();

        await modalService.Show(builder => builder.AddContent(0, "shown"), new BitModalParameters
        {
            Dir = BitDir.Ltr,
            IsEnabled = true,
            Visibility = BitVisibility.Visible,
        });

        // The base component marks the root off its own IsEnabled, Dir and Visibility, which the app-wide default was
        // written on: none of those marks may be left on a Modal whose showing asked for the opposite - an invisible
        // Modal that still holds the keyboard and the page, first of all.
        component.WaitForAssertion(() =>
        {
            var root = component.Find(".bit-mdl");
            Assert.AreEqual("ltr", root.GetAttribute("dir"));
            Assert.IsFalse(root.ClassList.Contains("bit-rtl"));
            Assert.IsFalse(root.ClassList.Contains("bit-dis"));
            Assert.IsFalse((root.GetAttribute("style") ?? string.Empty).Contains("visibility:hidden"));
        });
    }

    [TestMethod]
    public void BitModalShouldMarkItsRootOffTheCascadedBaseDefaults()
    {
        var component = RenderComponent<CascadingValue<BitModalParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitModalParams.ParamName);
            parameters.Add(p => p.Value, new BitModalParams { Dir = BitDir.Rtl, IsEnabled = false, Visibility = BitVisibility.Hidden });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitModal>(0);
                builder.AddComponentParameter(1, nameof(BitModal.IsOpen), true);
                builder.CloseComponent();
            }));
        });

        // With nothing to override them, the defaults mark the root once each.
        var root = component.Find(".bit-mdl");
        Assert.AreEqual(1, root.ClassList.Count(c => c == "bit-rtl"));
        Assert.AreEqual(1, root.ClassList.Count(c => c == "bit-dis"));
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
    }

    [TestMethod]
    public void BitModalShouldTakeCascadedBaseDefaultsItDidNotSetItself()
    {
        var component = RenderComponent<CascadingValue<BitModalParams>>(parameters =>
        {
            parameters.Add(p => p.Name, BitModalParams.ParamName);
            parameters.Add(p => p.Value, new BitModalParams { Dir = BitDir.Rtl, AriaLabel = "App-wide name" });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitModal>(0);
                builder.AddComponentParameter(1, nameof(BitModal.IsOpen), true);
                builder.AddComponentParameter(2, nameof(BitModal.AriaLabel), "Own name");
                builder.CloseComponent();
            }));
        });

        Assert.AreEqual("rtl", component.Find(".bit-mdl").GetAttribute("dir"));
        Assert.AreEqual("Own name", component.Find(".bit-mdl-ctn").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitModalParamsShouldCarryEveryPlainParameterOfTheComponent()
    {
        // A parameter added to the component without its counterpart here is one a BitParams cascade silently
        // ignores. Callbacks and templates are deliberately left out, and so are the parameters that only mean
        // something for one Modal: its open state, its text, the ids it points at, the element it scrolls and the
        // guard that knows what that one Modal has to lose.
        var paramsProperties = typeof(BitModalParams).GetProperties().Select(p => p.Name).ToHashSet();
        paramsProperties.UnionWith(
        [
            nameof(BitModal.IsOpen),
            nameof(BitModal.DefaultIsOpen),
            nameof(BitModal.HeaderText),
            nameof(BitModal.FooterText),
            nameof(BitModal.TitleAriaId),
            nameof(BitModal.SubtitleAriaId),
            nameof(BitModal.ScrollerElement),
            nameof(BitModal.CanClose),
        ]);

        var missing = typeof(BitModal).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                      .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                      .Where(p => p.PropertyType.Name.StartsWith("EventCallback") is false)
                                      .Where(p => p.PropertyType.Name.StartsWith("RenderFragment") is false)
                                      .Select(p => p.Name)
                                      .Where(n => paramsProperties.Contains(n) is false)
                                      .ToList();

        CollectionAssert.AreEqual(new List<string>(), missing, string.Join(", ", missing));
    }

    [TestMethod]
    public void BitModalParamsShouldApplyEveryPropertyItCarries()
    {
        // The other direction: a property added here but forgotten in UpdateParameters is accepted by the cascade
        // and then never reaches the Modal.
        var modalParams = new BitModalParams();
        var modal = new BitModal();

        var properties = typeof(BitModalParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                               .Where(p => p.CanWrite)
                                               .ToArray();

        foreach (var property in properties)
        {
            var target = typeof(BitModal).GetProperty(property.Name)!;

            property.SetValue(modalParams, SampleValue(property.PropertyType, target.GetValue(modal)));
        }

        modalParams.UpdateParameters(modal);

        foreach (var property in properties)
        {
            var target = typeof(BitModal).GetProperty(property.Name)!;

            Assert.AreEqual(property.GetValue(modalParams), target.GetValue(modal), $"{property.Name} is not applied.");
        }

        // A value unlike the component's default, so the assertion above cannot pass by coincidence.
        static object SampleValue(Type type, object? current)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying == typeof(bool)) return current is true ? false : true;
            if (underlying == typeof(string)) return "sample";
            if (underlying == typeof(BitIconInfo)) return BitIconInfo.Css("sample");
            if (underlying == typeof(BitModalClassStyles)) return new BitModalClassStyles();
            if (underlying.IsEnum)
            {
                var values = Enum.GetValues(underlying);
                return values.GetValue(values.Length - 1)!;
            }

            throw new NotSupportedException($"Add a sample value for {type}.");
        }
    }
}
