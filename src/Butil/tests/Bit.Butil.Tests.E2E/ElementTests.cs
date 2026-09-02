using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// The <see cref="Microsoft.AspNetCore.Components.ElementReference"/> extensions, driven against a
/// real element in a real engine.
/// </summary>
/// <remarks>
/// These wrappers are thin, which is exactly why they need a browser: nothing but a live DOM can
/// tell a correctly spelled property from one the engine ignores, and a misspelling on either side
/// of the interop boundary compiles cleanly and reads back as an empty string. Every case below
/// therefore round-trips - it writes through the wrapper and reads back through another one, or
/// through a state the DOM itself changes.
/// </remarks>
[TestClass]
public class ElementTests : ButilPageTest
{
    [TestMethod]
    public async Task Click_Runs_The_Elements_Own_Handler()
    {
        // The harness counts the Blazor @onclick, so this passing means the synthetic click really
        // dispatched rather than merely not throwing.
        await ClickAndExpectAsync("el-click", "el:click:1");
    }

    [TestMethod]
    public async Task Focus_Moves_Keyboard_Focus_To_The_Element()
    {
        // Read back through Matches(":focus") rather than through anything Butil wrote itself.
        await ClickAndExpectAsync("el-focus", "el:focus:True");
    }

    [TestMethod]
    public async Task CheckVisibility_Separates_A_Rendered_Element_From_A_Display_None_One()
    {
        await ClickAndExpectAsync("el-visibility", "el:vis:True/False");
    }

    [TestMethod]
    public async Task Closest_Reports_Matching_And_Non_Matching_Ancestors()
    {
        await ClickAndExpectAsync("el-closest", "el:closest:True/False");
    }

    [TestMethod]
    public async Task ClassList_Add_Contains_Replace_Toggle_And_Remove_Round_Trip()
    {
        // The element starts with one class; add two, replace one, toggle one back off, so what
        // survives is the original plus the replacement.
        await ClickAndExpectAsync("el-classes", "el:class:True/True/False/2");
    }

    [TestMethod]
    public async Task Dataset_Set_Get_Names_And_Remove_Round_Trip()
    {
        await ClickAndExpectAsync("el-data", "el:data:42/True/True");
    }

    [TestMethod]
    public async Task Inline_Style_Handles_Custom_Properties_And_Removal()
    {
        // A custom property proves the write goes through setProperty rather than through a
        // camel-cased CSSStyleDeclaration member, which cannot express "--butil-accent" at all.
        await ClickAndExpectAsync("el-style", "el:style:#123456/3px");
    }

    [TestMethod]
    public async Task Content_Insertion_Places_Text_In_The_Right_Order()
    {
        await ClickAndExpectAsync("el-content", "el:content:prepend+base+append!");
    }

    [TestMethod]
    public async Task Aria_Properties_And_Role_Round_Trip()
    {
        // aria-expanded reads back as the string "true": these are enumerated attributes, and an
        // absent one does not mean the same thing to a screen reader as "false".
        await ClickAndExpectAsync("el-aria", "el:aria:region/butil-e2e/true");
    }

    [TestMethod]
    public async Task Namespaced_Attributes_Round_Trip_And_Remove()
    {
        await ClickAndExpectAsync("el-ns", "el:ns:#star/True/False");
    }

    [TestMethod]
    public async Task Query_Helpers_Report_Matches_And_Counts()
    {
        await ClickAndExpectAsync("el-query", "el:query:True/3/0");
    }

    [TestMethod]
    public async Task Scroll_Offsets_Can_Be_Written_And_Their_Maximum_Read()
    {
        await ClickAndExpectAsync("el-scroll", "el:scroll:True/True");
    }

    [TestMethod]
    public async Task GetClientRects_Reports_At_Least_One_Laid_Out_Box()
    {
        await ClickAndExpectAsync("el-rects", "el:rects:True/True");
    }

    [TestMethod]
    public async Task Popover_Kind_Round_Trips_And_Toggle_Shows_It()
    {
        await ClickAndExpectAsync("el-popover", "el:popover:Manual/True");
    }

    [TestMethod]
    public async Task Identity_Hints_And_Tree_Facts_Read_Back()
    {
        await ClickAndExpectAsync("el-identity", "el:identity:butil-e2e-title/fa-IR/True/div/3/True");
    }
}
