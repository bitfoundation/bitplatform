using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Swiper;

[TestClass]
public class BitSwiperTests : BunitTestContext
{
    [TestInitialize]
    public void Init()
    {
        Services.AddScoped(_ => new BitPageVisibility(new TestJsRuntime()));
    }



    #region rendering

    [TestMethod]
    public void BitSwiperShouldRenderItemsAndControls()
    {
        var component = RenderComponent<BitSwiperTest>();

        Assert.AreEqual(3, component.FindAll(".bit-swpi").Count);

        Assert.IsNotNull(component.Find(".bit-swp-vwp"));
        Assert.IsNotNull(component.Find(".bit-swp-cnt"));

        // the next/prev controls are real buttons, so they can be reached and operated with the keyboard
        Assert.IsNotNull(component.Find("button.bit-swp-rbt"));
        Assert.IsNotNull(component.Find("button.bit-swp-lbt"));
    }

    [TestMethod]
    public void BitSwiperShouldHideNavigationWhenRequested()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.HideNextPrev, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-swp-lbt").Count);
        Assert.AreEqual(0, component.FindAll(".bit-swp-rbt").Count);
    }

    [TestMethod]
    public void BitSwiperShouldRenderAccessibilityAttributes()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Photos");
        });

        var root = component.Find(".bit-swp");

        Assert.AreEqual("region", root.GetAttribute("role"));
        Assert.AreEqual("carousel", root.GetAttribute("aria-roledescription"));
        Assert.AreEqual("Photos", root.GetAttribute("aria-label"));
        Assert.AreEqual("0", root.GetAttribute("tabindex"));

        var items = component.FindAll(".bit-swpi");

        foreach (var (item, index) in items.Select((item, index) => (item, index)))
        {
            Assert.AreEqual("group", item.GetAttribute("role"));
            Assert.AreEqual("slide", item.GetAttribute("aria-roledescription"));
            Assert.AreEqual($"{index + 1} of 3", item.GetAttribute("aria-label"));
        }
    }

    [TestMethod]
    public void BitSwiperShouldRespectItemAriaLabelFormat()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.ItemAriaLabelFormat, "Photo {0} of {1}");
        });

        var items = component.FindAll(".bit-swpi");

        Assert.AreEqual("Photo 1 of 3", items[0].GetAttribute("aria-label"));
        Assert.AreEqual("Photo 3 of 3", items[2].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSwiperItemShouldKeepItsOwnAriaLabel()
    {
        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.AddChildContent<BitSwiperItem>(item => item.Add(p => p.AriaLabel, "The aurora"));
        });

        Assert.AreEqual("The aurora", component.Find(".bit-swpi").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSwiperShouldPointControlsAtTheContainer()
    {
        var component = RenderComponent<BitSwiperTest>();

        var containerId = component.Find(".bit-swp-cnt").GetAttribute("id");

        Assert.IsFalse(string.IsNullOrEmpty(containerId));
        Assert.AreEqual(containerId, component.Find(".bit-swp-rbt").GetAttribute("aria-controls"));
        Assert.AreEqual(containerId, component.Find(".bit-swp-lbt").GetAttribute("aria-controls"));
    }

    [TestMethod]
    public void BitSwiperShouldRenderDefaultNavigationAriaLabels()
    {
        var component = RenderComponent<BitSwiperTest>();

        Assert.AreEqual("Next slide", component.Find(".bit-swp-rbt").GetAttribute("aria-label"));
        Assert.AreEqual("Previous slide", component.Find(".bit-swp-lbt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSwiperShouldRespectCustomNavigationAriaLabels()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.NextAriaLabel, "Next photo");
            parameters.Add(p => p.PrevAriaLabel, "Previous photo");
        });

        Assert.AreEqual("Next photo", component.Find(".bit-swp-rbt").GetAttribute("aria-label"));
        Assert.AreEqual("Previous photo", component.Find(".bit-swp-lbt").GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitSwiperShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var root = component.Find(".bit-swp");

        Assert.AreEqual(isEnabled is false, root.ClassList.Contains("bit-dis"));

        // a disabled swiper is neither focusable nor operable
        Assert.AreEqual(isEnabled ? "0" : null, root.GetAttribute("tabindex"));
        Assert.AreEqual(isEnabled is false, component.Find(".bit-swp-rbt").HasAttribute("disabled"));
        Assert.AreEqual(isEnabled is false, component.Find(".bit-swp-lbt").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitSwiperShouldNotBeFocusableWhenNoKeyboard()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.NoKeyboard, true);
        });

        Assert.IsNull(component.Find(".bit-swp").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitSwiperShouldRespectRtlDirection()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-swp");

        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));

        var containerStyle = component.Find(".bit-swp-cnt").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(containerStyle.Contains("direction:rtl"));
    }

    #endregion



    #region icons

    [TestMethod]
    public void BitSwiperShouldRenderDefaultIconsWithRotationOnPrev()
    {
        var component = RenderComponent<BitSwiperTest>();

        var nextIcon = component.Find(".bit-swp-rbt i");
        var prevIcon = component.Find(".bit-swp-lbt i");

        Assert.IsTrue(nextIcon.ClassList.Contains("bit-icon--ChevronRight"));
        Assert.IsFalse(nextIcon.ClassList.Contains("bit-ico-r180"));
        Assert.IsTrue(prevIcon.ClassList.Contains("bit-icon--ChevronRight"));
        Assert.IsTrue(prevIcon.ClassList.Contains("bit-ico-r180"));

        // the icons are decorative: the buttons already carry the accessible names
        Assert.AreEqual("true", nextIcon.GetAttribute("aria-hidden"));
        Assert.AreEqual("true", prevIcon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitSwiperShouldFlipDefaultIconsInRtl()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var nextIcon = component.Find(".bit-swp-rbt i");
        var prevIcon = component.Find(".bit-swp-lbt i");

        Assert.IsTrue(nextIcon.ClassList.Contains("bit-ico-r180"));
        Assert.IsFalse(prevIcon.ClassList.Contains("bit-ico-r180"));
    }

    [TestMethod]
    public void BitSwiperShouldRenderVerticalDefaultIcons()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        Assert.IsTrue(component.Find(".bit-swp-rbt i").ClassList.Contains("bit-icon--ChevronDown"));
        Assert.IsTrue(component.Find(".bit-swp-lbt i").ClassList.Contains("bit-icon--ChevronUp"));
    }

    [TestMethod]
    public void BitSwiperShouldRenderCorrectNextIconNameWhenProvided()
    {
        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.NextIconName, "ChevronRightSmall");
        });

        var nextIcon = component.Find(".bit-swp-rbt i");

        Assert.IsTrue(nextIcon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(nextIcon.ClassList.Contains("bit-icon--ChevronRightSmall"));
    }

    [TestMethod]
    public void BitSwiperShouldRenderCorrectPrevIconNameWhenProvided()
    {
        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.PrevIconName, "ChevronLeft");
        });

        var prevIcon = component.Find(".bit-swp-lbt i");

        Assert.IsTrue(prevIcon.ClassList.Contains("bit-icon"));
        Assert.IsTrue(prevIcon.ClassList.Contains("bit-icon--ChevronLeft"));
    }

    [TestMethod]
    public void BitSwiperShouldRenderCorrectNextIconWhenProvided()
    {
        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.NextIcon, BitIconInfo.Fa("solid chevron-right"));
        });

        var nextIcon = component.Find(".bit-swp-rbt i");

        Assert.IsTrue(nextIcon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(nextIcon.ClassList.Contains("fa-chevron-right"));
    }

    [TestMethod]
    public void BitSwiperShouldRenderCorrectPrevIconWhenProvided()
    {
        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.PrevIcon, BitIconInfo.Fa("solid chevron-left"));
        });

        var prevIcon = component.Find(".bit-swp-lbt i");

        Assert.IsTrue(prevIcon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(prevIcon.ClassList.Contains("fa-chevron-left"));
    }

    [TestMethod]
    public void BitSwiperNextIconShouldTakePrecedenceOverNextIconName()
    {
        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.NextIcon, BitIconInfo.Bi("arrow-right-circle"));
            parameters.Add(p => p.NextIconName, "ChevronRight");
        });

        var nextIcon = component.Find(".bit-swp-rbt i");

        Assert.IsTrue(nextIcon.ClassList.Contains("bi-arrow-right-circle"));
        Assert.IsFalse(nextIcon.ClassList.Contains("bit-icon--ChevronRight"));
    }

    [TestMethod]
    public void BitSwiperPrevIconShouldTakePrecedenceOverPrevIconName()
    {
        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.Add(p => p.PrevIcon, BitIconInfo.Bi("arrow-left-circle"));
            parameters.Add(p => p.PrevIconName, "ChevronLeft");
        });

        var prevIcon = component.Find(".bit-swp-lbt i");

        Assert.IsTrue(prevIcon.ClassList.Contains("bi-arrow-left-circle"));
        Assert.IsFalse(prevIcon.ClassList.Contains("bit-icon--ChevronLeft"));
    }

    #endregion



    #region look and feel

    [TestMethod]
    public void BitSwiperShouldRespectVertical()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        Assert.IsTrue(component.Find(".bit-swp").ClassList.Contains("bit-swp-vrt"));
    }

    [TestMethod,
        DataRow(BitSwiperSnap.Start, "bit-swp-sns"),
        DataRow(BitSwiperSnap.Center, "bit-swp-snc"),
        DataRow(BitSwiperSnap.End, "bit-swp-sne")]
    public void BitSwiperShouldRespectSnap(BitSwiperSnap snap, string expectedClass)
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Snap, snap);
        });

        var root = component.Find(".bit-swp");

        Assert.IsTrue(root.ClassList.Contains("bit-swp-snp"));
        Assert.IsTrue(root.ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitSwiperShouldNotSnapByDefault()
    {
        var component = RenderComponent<BitSwiperTest>();

        Assert.IsFalse(component.Find(".bit-swp").ClassList.Contains("bit-swp-snp"));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-swp-sm"),
        DataRow(BitSize.Medium, "bit-swp-md"),
        DataRow(BitSize.Large, "bit-swp-lg")]
    public void BitSwiperShouldRespectSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-swp").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-swp-pri"),
        DataRow(BitColor.Success, "bit-swp-suc"),
        DataRow(BitColor.Error, "bit-swp-err"),
        DataRow(BitColor.SecondaryBorder, "bit-swp-sbr")]
    public void BitSwiperShouldRespectColor(BitColor color, string expectedClass)
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-swp").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitColorKind.Primary, "bit-swp-apri"),
        DataRow(BitColorKind.Secondary, "bit-swp-asec"),
        DataRow(BitColorKind.Tertiary, "bit-swp-ater"),
        DataRow(BitColorKind.Transparent, "bit-swp-atra")]
    public void BitSwiperShouldRespectAccent(BitColorKind accent, string expectedClass)
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Accent, accent);
        });

        Assert.IsTrue(component.Find(".bit-swp").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitSwiperShouldRespectShowScrollbar()
    {
        var component = RenderComponent<BitSwiperTest>();

        Assert.IsFalse(component.Find(".bit-swp").ClassList.Contains("bit-swp-scb"));

        component.Render(parameters => parameters.Add(p => p.ShowScrollbar, true));

        Assert.IsTrue(component.Find(".bit-swp").ClassList.Contains("bit-swp-scb"));
    }

    [TestMethod]
    public void BitSwiperShouldRespectNoDrag()
    {
        var component = RenderComponent<BitSwiperTest>();

        Assert.IsFalse(component.Find(".bit-swp").ClassList.Contains("bit-swp-ndr"));

        component.Render(parameters => parameters.Add(p => p.NoDrag, true));

        Assert.IsTrue(component.Find(".bit-swp").ClassList.Contains("bit-swp-ndr"));
    }

    [TestMethod]
    public void BitSwiperShouldRespectGap()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Gap, "1rem");
        });

        Assert.IsTrue((component.Find(".bit-swp").GetAttribute("style") ?? string.Empty).Contains("--bit-swp-gap:1rem"));
    }

    [TestMethod]
    public void BitSwiperShouldNotSizeItemsWithoutVisibleItemsCount()
    {
        var component = RenderComponent<BitSwiperTest>();

        Assert.IsFalse((component.Find(".bit-swp").GetAttribute("style") ?? string.Empty).Contains("--bit-swp-isz"));
    }

    [TestMethod]
    public void BitSwiperShouldSizeItemsFromVisibleItemsCount()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.VisibleItemsCount, 4);
            parameters.Add(p => p.Gap, "1rem");
        });

        var style = component.Find(".bit-swp").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-swp-isz:calc((100% - 3 * var(--bit-swp-gap, 0px)) / 4)"));
    }

    [TestMethod]
    public void BitSwiperShouldSizeASingleVisibleItemToTheWholeSwiper()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.VisibleItemsCount, 1);
        });

        Assert.IsTrue((component.Find(".bit-swp").GetAttribute("style") ?? string.Empty).Contains("--bit-swp-isz:100%"));
    }

    [TestMethod]
    public async Task BitSwiperShouldResolveResponsiveVisibleItemsCount()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.VisibleItemsCount, 1);
            parameters.Add(p => p.VisibleItemsCountSm, 2);
            parameters.Add(p => p.VisibleItemsCountMd, 3);
        });

        // below the small breakpoint the base value applies
        await PushState(component, viewport: 500);
        Assert.IsTrue((component.Find(".bit-swp").GetAttribute("style") ?? string.Empty).Contains("--bit-swp-isz:100%"));

        // between the small and the medium breakpoint the small variant applies
        await PushState(component, viewport: 700);
        Assert.IsTrue((component.Find(".bit-swp").GetAttribute("style") ?? string.Empty).Contains("/ 2)"));

        // past the medium breakpoint the largest variant that was set wins
        await PushState(component, viewport: 1000);
        Assert.IsTrue((component.Find(".bit-swp").GetAttribute("style") ?? string.Empty).Contains("/ 3)"));
    }

    [TestMethod]
    public void BitSwiperShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitSwiperClassStyles
            {
                Root = "custom-root",
                Container = "custom-container",
                Item = "custom-item",
                Buttons = "custom-buttons",
                NextButton = "custom-next",
                PrevButton = "custom-prev",
                ButtonIcons = "custom-icons"
            });
            parameters.Add(p => p.Styles, new BitSwiperClassStyles
            {
                Root = "z-index:5",
                Container = "padding:1px",
                NextButton = "opacity:0.7"
            });
        });

        var root = component.Find(".bit-swp");

        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        Assert.IsTrue((root.GetAttribute("style") ?? string.Empty).Contains("z-index:5"));

        var container = component.Find(".bit-swp-cnt");

        Assert.IsTrue(container.ClassList.Contains("custom-container"));
        Assert.IsTrue((container.GetAttribute("style") ?? string.Empty).Contains("padding:1px"));

        var next = component.Find(".bit-swp-rbt");

        Assert.IsTrue(next.ClassList.Contains("custom-buttons"));
        Assert.IsTrue(next.ClassList.Contains("custom-next"));
        Assert.IsTrue((next.GetAttribute("style") ?? string.Empty).Contains("opacity:0.7"));

        Assert.IsTrue(component.Find(".bit-swp-lbt").ClassList.Contains("custom-prev"));
        Assert.IsTrue(component.Find(".bit-swp-rbt i").ClassList.Contains("custom-icons"));
        Assert.IsTrue(component.Find(".bit-swpi").ClassList.Contains("custom-item"));
    }

    [TestMethod]
    public async Task BitSwiperShouldMarkTheCurrentItem()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitSwiperClassStyles { CurrentItem = "custom-current" });
            parameters.Add(p => p.Styles, new BitSwiperClassStyles { CurrentItem = "outline:1px solid red" });
        });

        await PushState(component, index: 1);

        var items = component.FindAll(".bit-swpi");

        Assert.IsFalse(items[0].ClassList.Contains("bit-swpi-cur"));
        Assert.IsTrue(items[1].ClassList.Contains("bit-swpi-cur"));
        Assert.IsTrue(items[1].ClassList.Contains("custom-current"));
        Assert.IsTrue((items[1].GetAttribute("style") ?? string.Empty).Contains("outline:1px solid red"));
    }

    #endregion



    #region dots and play/pause

    [TestMethod]
    public void BitSwiperShouldNotRenderDotsByDefault()
    {
        var component = RenderComponent<BitSwiperTest>();

        Assert.AreEqual(0, component.FindAll(".bit-swp-dot").Count);
    }

    [TestMethod]
    public async Task BitSwiperShouldRenderDotsOncePagesAreKnown()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.ShowDots, true);
        });

        // nothing is known about the pages before the swiper has been measured
        Assert.AreEqual(0, component.FindAll(".bit-swp-dot").Count);

        await PushState(component, page: 1, pagesCount: 3);

        var dots = component.FindAll("button.bit-swp-dot");

        Assert.AreEqual(3, dots.Count);
        Assert.AreEqual("Slide 1", dots[0].GetAttribute("aria-label"));
        Assert.IsNull(dots[0].GetAttribute("aria-current"));
        Assert.AreEqual("true", dots[1].GetAttribute("aria-current"));
        Assert.IsTrue(dots[1].ClassList.Contains("bit-swp-cud"));

        var group = component.Find(".bit-swp-dts");

        Assert.AreEqual("group", group.GetAttribute("role"));
        Assert.AreEqual("Choose slide to display", group.GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task BitSwiperShouldNotRenderDotsForASinglePage()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.ShowDots, true);
        });

        await PushState(component, pagesCount: 1, scrollable: false);

        Assert.AreEqual(0, component.FindAll(".bit-swp-dot").Count);
    }

    [TestMethod]
    public async Task BitSwiperShouldRespectCustomDotAriaLabels()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.ShowDots, true);
            parameters.Add(p => p.DotAriaLabel, "Page");
            parameters.Add(p => p.DotsAriaLabel, "Choose a page");
        });

        await PushState(component, pagesCount: 2);

        Assert.AreEqual("Page 2", component.FindAll(".bit-swp-dot")[1].GetAttribute("aria-label"));
        Assert.AreEqual("Choose a page", component.Find(".bit-swp-dts").GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task BitSwiperShouldRenderTheDotTemplate()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.ShowDots, true);
            parameters.Add(p => p.DotTemplate, (int index) => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(1, index + 1);
                builder.CloseElement();
            });
        });

        await PushState(component, pagesCount: 2);

        var dots = component.FindAll(".bit-swp-dot");

        Assert.IsTrue(dots[0].ClassList.Contains("bit-swp-dtt"));
        Assert.AreEqual("1", dots[0].TextContent);
        Assert.AreEqual("2", dots[1].TextContent);
    }

    [TestMethod]
    public async Task BitSwiperDotShouldNavigateToItsPage()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.ShowDots, true);
        });

        await PushState(component, pagesCount: 3);

        component.FindAll(".bit-swp-dot")[2].Click();

        var invocation = LastInvocation("BitBlazorUI.Swiper.goToPage");

        Assert.AreEqual(2, invocation.Arguments[1]);
    }

    [TestMethod]
    public async Task BitSwiperShouldRenderThePlayPauseButtonOnlyWithAutoPlay()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.ShowPlayPause, true);
            parameters.Add(p => p.AutoPlayInterval, 60000);
        });

        Assert.AreEqual(0, component.FindAll(".bit-swp-ppb").Count);

        component.Render(parameters => parameters.Add(p => p.AutoPlay, true));

        await PushState(component);

        var button = component.Find("button.bit-swp-ppb");

        Assert.AreEqual("Stop automatic slide show", button.GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-swp-ppb i").ClassList.Contains("bit-icon--Pause"));

        button.Click();

        Assert.IsTrue(component.Instance.Swiper.IsPaused);
        Assert.AreEqual("Start automatic slide show", component.Find(".bit-swp-ppb").GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-swp-ppb i").ClassList.Contains("bit-icon--Play"));
    }

    [TestMethod]
    public async Task BitSwiperShouldPauseAndResumeFromCode()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.AutoPlayInterval, 60000);
        });

        await PushState(component);

        var swiper = component.Instance.Swiper;

        Assert.IsTrue(swiper.IsPlaying);
        Assert.IsFalse(swiper.IsPaused);

        await component.InvokeAsync(swiper.Pause);

        Assert.IsFalse(swiper.IsPlaying);
        Assert.IsTrue(swiper.IsPaused);

        await component.InvokeAsync(swiper.Resume);

        Assert.IsTrue(swiper.IsPlaying);
        Assert.IsFalse(swiper.IsPaused);

        await component.InvokeAsync(swiper.TogglePlay);

        Assert.IsTrue(swiper.IsPaused);
    }

    [TestMethod]
    public async Task BitSwiperShouldNotPlayWhenThereIsNothingToScroll()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.AutoPlayInterval, 60000);
        });

        await PushState(component, scrollable: false, atEnd: true);

        Assert.IsFalse(component.Instance.Swiper.IsPlaying);
    }

    [TestMethod]
    public async Task BitSwiperShouldPauseOnHoverAndFocus()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.AutoPlayInterval, 60000);
        });

        await PushState(component);

        var swiper = component.Instance.Swiper;
        var root = component.Find(".bit-swp");

        Assert.IsTrue(swiper.IsPlaying);

        root.MouseEnter();
        Assert.IsFalse(swiper.IsPlaying);

        root.MouseLeave();
        Assert.IsTrue(swiper.IsPlaying);

        root.FocusIn();
        Assert.IsFalse(swiper.IsPlaying);

        root.FocusOut();
        Assert.IsTrue(swiper.IsPlaying);
    }

    [TestMethod]
    public async Task BitSwiperShouldStopOnInteractionWhenRequested()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.AutoPlayInterval, 60000);
            parameters.Add(p => p.StopOnInteraction, true);
            parameters.Add(p => p.PauseOnHover, false);
            parameters.Add(p => p.PauseOnFocus, false);
        });

        await PushState(component);

        Assert.IsTrue(component.Instance.Swiper.IsPlaying);

        component.Find(".bit-swp-rbt").Click();

        Assert.IsFalse(component.Instance.Swiper.IsPlaying);
        Assert.IsTrue(component.Instance.Swiper.IsPaused);
    }

    #endregion



    #region navigation

    [TestMethod]
    public void BitSwiperNextAndPrevButtonsShouldNavigate()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.ScrollItemsCount, 3);
        });

        component.Find(".bit-swp-rbt").Click();

        var next = LastInvocation("BitBlazorUI.Swiper.go");

        Assert.AreEqual(true, next.Arguments[1]);
        Assert.AreEqual(3, next.Arguments[2]);

        component.Find(".bit-swp-lbt").Click();

        var prev = LastInvocation("BitBlazorUI.Swiper.go");

        Assert.AreEqual(false, prev.Arguments[1]);
        Assert.AreEqual(3, prev.Arguments[2]);
    }

    [TestMethod]
    public void BitSwiperShouldClampScrollItemsCountToAtLeastOne()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.ScrollItemsCount, 0);
        });

        component.Find(".bit-swp-rbt").Click();

        Assert.AreEqual(1, LastInvocation("BitBlazorUI.Swiper.go").Arguments[2]);
    }

    [TestMethod]
    public async Task BitSwiperShouldNavigateFromThePublicApi()
    {
        var component = RenderComponent<BitSwiperTest>();

        var swiper = component.Instance.Swiper;

        await component.InvokeAsync(swiper.GoNext);
        Assert.AreEqual(true, LastInvocation("BitBlazorUI.Swiper.go").Arguments[1]);

        await component.InvokeAsync(swiper.GoPrev);
        Assert.AreEqual(false, LastInvocation("BitBlazorUI.Swiper.go").Arguments[1]);

        await component.InvokeAsync(() => swiper.GoTo(2));
        Assert.AreEqual(1, LastInvocation("BitBlazorUI.Swiper.goToItem").Arguments[1]);

        await component.InvokeAsync(swiper.GoToStart);
        Assert.AreEqual(false, LastInvocation("BitBlazorUI.Swiper.goToEdge").Arguments[1]);

        await component.InvokeAsync(swiper.GoToEnd);
        Assert.AreEqual(true, LastInvocation("BitBlazorUI.Swiper.goToEdge").Arguments[1]);

        await component.InvokeAsync(swiper.Refresh);
        Context.JSInterop.VerifyInvoke("BitBlazorUI.Swiper.refresh");
    }

    [TestMethod]
    public async Task BitSwiperGoToShouldClampToTheRangeOfTheSwiper()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 4);
        });

        var swiper = component.Instance.Swiper;

        await component.InvokeAsync(() => swiper.GoTo(99));
        Assert.AreEqual(3, LastInvocation("BitBlazorUI.Swiper.goToItem").Arguments[1]);

        await component.InvokeAsync(() => swiper.GoTo(-5));
        Assert.AreEqual(0, LastInvocation("BitBlazorUI.Swiper.goToItem").Arguments[1]);
    }

    [TestMethod]
    public async Task BitSwiperGoToPageShouldClampToTheRangeOfTheSwiper()
    {
        var component = RenderComponent<BitSwiperTest>();

        await PushState(component, pagesCount: 3);

        await component.InvokeAsync(() => component.Instance.Swiper.GoToPage(99));
        Assert.AreEqual(2, LastInvocation("BitBlazorUI.Swiper.goToPage").Arguments[1]);

        await component.InvokeAsync(() => component.Instance.Swiper.GoToPage(-5));
        Assert.AreEqual(0, LastInvocation("BitBlazorUI.Swiper.goToPage").Arguments[1]);
    }

    [TestMethod]
    public async Task BitSwiperShouldNotNavigateWhenDisabled()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        await component.InvokeAsync(component.Instance.Swiper.GoNext);
        await component.InvokeAsync(() => component.Instance.Swiper.GoTo(2));
        await component.InvokeAsync(component.Instance.Swiper.GoToEnd);

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier.StartsWith("BitBlazorUI.Swiper.go")));
    }

    [TestMethod,
        DataRow("ArrowRight", true),
        DataRow("ArrowLeft", false)]
    public void BitSwiperShouldNavigateWithTheArrowKeys(string key, bool expectedForward)
    {
        var component = RenderComponent<BitSwiperTest>();

        component.Find(".bit-swp").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expectedForward, LastInvocation("BitBlazorUI.Swiper.go").Arguments[1]);
    }

    [TestMethod,
        DataRow("ArrowRight", false),
        DataRow("ArrowLeft", true)]
    public void BitSwiperShouldFlipTheArrowKeysInRtl(string key, bool expectedForward)
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        component.Find(".bit-swp").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expectedForward, LastInvocation("BitBlazorUI.Swiper.go").Arguments[1]);
    }

    [TestMethod,
        DataRow("ArrowDown", true),
        DataRow("ArrowUp", false)]
    public void BitSwiperShouldNavigateWithTheVerticalArrowKeys(string key, bool expectedForward)
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        component.Find(".bit-swp").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expectedForward, LastInvocation("BitBlazorUI.Swiper.go").Arguments[1]);
    }

    [TestMethod,
        DataRow("Home", false),
        DataRow("End", true)]
    public void BitSwiperShouldJumpToTheEndsWithHomeAndEnd(string key, bool expectedEnd)
    {
        var component = RenderComponent<BitSwiperTest>();

        component.Find(".bit-swp").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expectedEnd, LastInvocation("BitBlazorUI.Swiper.goToEdge").Arguments[1]);
    }

    [TestMethod]
    public async Task BitSwiperShouldNavigateByPageWithPageUpAndPageDown()
    {
        var component = RenderComponent<BitSwiperTest>();

        await PushState(component, page: 1, pagesCount: 4);

        component.Find(".bit-swp").KeyDown(new KeyboardEventArgs { Key = "PageDown" });
        Assert.AreEqual(2, LastInvocation("BitBlazorUI.Swiper.goToPage").Arguments[1]);

        component.Find(".bit-swp").KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        Assert.AreEqual(0, LastInvocation("BitBlazorUI.Swiper.goToPage").Arguments[1]);
    }

    [TestMethod,
        DataRow("Ctrl"),
        DataRow("Alt"),
        DataRow("Shift"),
        DataRow("Meta")]
    public void BitSwiperShouldIgnoreModifiedKeys(string modifier)
    {
        var component = RenderComponent<BitSwiperTest>();

        component.Find(".bit-swp").KeyDown(new KeyboardEventArgs
        {
            Key = "ArrowRight",
            CtrlKey = modifier == "Ctrl",
            AltKey = modifier == "Alt",
            ShiftKey = modifier == "Shift",
            MetaKey = modifier == "Meta"
        });

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Swiper.go"));
    }

    [TestMethod]
    public void BitSwiperShouldNotNavigateWithTheKeyboardWhenNoKeyboard()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.NoKeyboard, true);
        });

        component.Find(".bit-swp").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Swiper.go"));
    }

    [TestMethod]
    public async Task BitSwiperShouldHideEachButtonAtTheEndItCannotPass()
    {
        var component = RenderComponent<BitSwiperTest>();

        await PushState(component, atStart: true, atEnd: false);

        Assert.IsTrue((component.Find(".bit-swp-lbt").GetAttribute("style") ?? string.Empty).Contains("display:none"));
        Assert.IsFalse((component.Find(".bit-swp-rbt").GetAttribute("style") ?? string.Empty).Contains("display:none"));

        await PushState(component, index: 2, atStart: false, atEnd: true);

        Assert.IsFalse((component.Find(".bit-swp-lbt").GetAttribute("style") ?? string.Empty).Contains("display:none"));
        Assert.IsTrue((component.Find(".bit-swp-rbt").GetAttribute("style") ?? string.Empty).Contains("display:none"));
    }

    [TestMethod]
    public async Task BitSwiperShouldHideBothButtonsWhenNothingCanBeScrolled()
    {
        var component = RenderComponent<BitSwiperTest>();

        await PushState(component, atStart: true, atEnd: true, scrollable: false);

        Assert.IsTrue((component.Find(".bit-swp-lbt").GetAttribute("style") ?? string.Empty).Contains("display:none"));
        Assert.IsTrue((component.Find(".bit-swp-rbt").GetAttribute("style") ?? string.Empty).Contains("display:none"));
    }

    #endregion



    #region state and interop

    [TestMethod]
    public void BitSwiperShouldSetupJsInteropOnFirstRender()
    {
        RenderComponent<BitSwiperTest>();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Swiper.setup");

        var keys = (string[])LastInvocation("BitBlazorUI.Utils.registerPreventKeys").Arguments[1]!;

        CollectionAssert.AreEquivalent(new[] { "ArrowLeft", "ArrowRight", "Home", "End", "PageUp", "PageDown" }, keys);
    }

    [TestMethod]
    public void BitSwiperShouldPreventTheVerticalKeysWhenVertical()
    {
        RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
        });

        var keys = (string[])LastInvocation("BitBlazorUI.Utils.registerPreventKeys").Arguments[1]!;

        CollectionAssert.AreEquivalent(new[] { "ArrowUp", "ArrowDown", "Home", "End", "PageUp", "PageDown" }, keys);
    }

    [TestMethod]
    public void BitSwiperShouldPreventNoKeysWhenNoKeyboard()
    {
        RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.NoKeyboard, true);
        });

        var keys = (string[])LastInvocation("BitBlazorUI.Utils.registerPreventKeys").Arguments[1]!;

        Assert.AreEqual(0, keys.Length);
    }

    [TestMethod]
    public void BitSwiperShouldHandItsOptionsToTheBrowser()
    {
        RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.NoDrag, true);
            parameters.Add(p => p.Wheel, true);
            parameters.Add(p => p.Snap, BitSwiperSnap.Center);
            parameters.Add(p => p.DefaultItem, 3);
            parameters.Add(p => p.DragThreshold, 12);
            parameters.Add(p => p.ScrollItemsCount, 2);
            parameters.Add(p => p.AnimationDuration, 0.25);
        });

        var options = LastInvocation("BitBlazorUI.Swiper.setup").Arguments[4]!;

        Assert.AreEqual(true, ReadOption(options, "Vertical"));
        Assert.AreEqual(true, ReadOption(options, "NoDrag"));
        Assert.AreEqual(true, ReadOption(options, "Wheel"));
        Assert.AreEqual(true, ReadOption(options, "Enabled"));
        Assert.AreEqual(true, ReadOption(options, "Snap"));
        Assert.AreEqual(0.5, ReadOption(options, "Align"));
        Assert.AreEqual(0.25, ReadOption(options, "Duration"));
        Assert.AreEqual(12, ReadOption(options, "Threshold"));
        Assert.AreEqual(2, ReadOption(options, "ScrollCount"));

        // DefaultItem is 1 based, and reaches the browser as the zero based index it lays the swiper out on
        Assert.AreEqual(2, ReadOption(options, "Start"));
    }

    [TestMethod]
    public void BitSwiperShouldAlignToTheStartWhenItDoesNotSnap()
    {
        RenderComponent<BitSwiperTest>();

        var options = LastInvocation("BitBlazorUI.Swiper.setup").Arguments[4]!;

        Assert.AreEqual(false, ReadOption(options, "Snap"));
        Assert.AreEqual(0d, ReadOption(options, "Align"));
        Assert.AreEqual(0, ReadOption(options, "Start"));
    }

    [TestMethod]
    public void BitSwiperShouldUpdateItsOptionsWhenTheyChange()
    {
        var component = RenderComponent<BitSwiperTest>();

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Swiper.update"));

        component.Render(parameters => parameters.Add(p => p.Wheel, true));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Swiper.update");
    }

    [TestMethod]
    public void BitSwiperShouldNotUpdateItsOptionsForAnUnrelatedChange()
    {
        var component = RenderComponent<BitSwiperTest>();

        component.Render(parameters => parameters.Add(p => p.AriaLabel, "Photos"));

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Swiper.update"));
    }

    [TestMethod]
    public void BitSwiperShouldRefreshWhenItsItemsChange()
    {
        var component = RenderComponent<BitSwiperTest>();

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Swiper.refresh"));

        component.Render(parameters => parameters.Add(p => p.ItemsCount, 5));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Swiper.refresh");
        Assert.AreEqual(5, component.Instance.Swiper.ItemsCount);
    }

    [TestMethod]
    public void BitSwiperShouldReindexItsItemsWhenSomeAreRemoved()
    {
        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 4);
        });

        Assert.AreEqual(4, component.Instance.Swiper.ItemsCount);

        component.Render(parameters => parameters.Add(p => p.ItemsCount, 2));

        Assert.AreEqual(2, component.Instance.Swiper.ItemsCount);

        var items = component.FindAll(".bit-swpi");

        Assert.AreEqual(2, items.Count);
        Assert.AreEqual("1 of 2", items[0].GetAttribute("aria-label"));
        Assert.AreEqual("2 of 2", items[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task BitSwiperShouldReportWhereItStands()
    {
        var component = RenderComponent<BitSwiperTest>();

        var swiper = component.Instance.Swiper;

        Assert.AreEqual(0, swiper.CurrentIndex);
        Assert.IsTrue(swiper.IsAtStart);

        await PushState(component, index: 2, page: 1, pagesCount: 4, atStart: false, atEnd: true);

        Assert.AreEqual(2, swiper.CurrentIndex);
        Assert.AreEqual(1, swiper.CurrentPage);
        Assert.AreEqual(4, swiper.PagesCount);
        Assert.AreEqual(3, swiper.ItemsCount);
        Assert.IsFalse(swiper.IsAtStart);
        Assert.IsTrue(swiper.IsAtEnd);
    }

    [TestMethod]
    public async Task BitSwiperShouldFireOnChangeOnlyWhenTheItemChanges()
    {
        var changes = new List<int>();

        var component = RenderComponent<BitSwiperTest>(parameters =>
        {
            parameters.Add(p => p.OnChange, index => changes.Add(index));
        });

        await PushState(component, index: 1);
        await PushState(component, index: 1, page: 1);
        await PushState(component, index: 2);

        CollectionAssert.AreEqual(new[] { 1, 2 }, changes);
    }

    [TestMethod]
    public async Task BitSwiperShouldDisposeJsInteropOnDispose()
    {
        var component = RenderComponent<BitSwiperTest>();

        await component.Instance.Swiper.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Swiper.dispose");
    }

    #endregion



    private static Task PushState(IRenderedComponent<BitSwiperTest> component,
                                  int index = 0,
                                  int page = 0,
                                  int pagesCount = 2,
                                  bool atStart = true,
                                  bool atEnd = false,
                                  bool scrollable = true,
                                  double viewport = 600)
    {
        return component.InvokeAsync(() => component.Instance.Swiper._OnStateChange(new BitSwiperState
        {
            Index = index,
            Page = page,
            PagesCount = pagesCount,
            AtStart = atStart,
            AtEnd = atEnd,
            Scrollable = scrollable,
            Viewport = viewport
        }));
    }

    private JSRuntimeInvocation LastInvocation(string identifier)
    {
        return Context.JSInterop.Invocations.Last(i => i.Identifier == identifier);
    }

    // The options the swiper hands to the browser are an internal type, so they are read back by name.
    private static object? ReadOption(object options, string name)
    {
        // A member that was renamed away would otherwise read as null and quietly pass the assertions
        // that compare it against a default of the same shape.
        var property = options.GetType().GetProperty(name)
            ?? throw new InvalidOperationException($"The '{name}' option is missing from {options.GetType().Name}.");

        return property.GetValue(options);
    }
}
