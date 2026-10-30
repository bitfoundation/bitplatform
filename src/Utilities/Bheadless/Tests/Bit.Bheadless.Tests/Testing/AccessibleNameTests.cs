using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Bit.Bheadless.Tests.Testing;

/// <summary>The accessible name and description computation (accname 1.2, HTML-AAM) of Bit.Bheadless.Testing.</summary>
[TestClass]
public class AccessibleNameTests
{
    private static readonly HtmlParser Parser = new();

    private static IElement Body(string html) => Parser.ParseDocument($"<body>{html}</body>").Body!;

    private static string Name(string html) => Body(html).QuerySelector("#t")!.AccessibleName();

    private static string Description(string html) => Body(html).QuerySelector("#t")!.AccessibleDescription();

    [TestMethod]
    public void Labelledby_is_not_followed_again_inside_a_labelledby_traversal()
    {
        // The heading the button points at contains the button, whose own labelledby points back at the heading.
        const string html = "<h3 id='h'>Item <button id='t' aria-labelledby='t h'>Delete</button></h3>";
        Assert.AreEqual("Delete Item Delete", Name(html));
        Assert.AreEqual("Item Delete Item Delete", Body(html).QuerySelector("#h")!.AccessibleName());
        var report = BitBhA11y.Audit(Body(html));
        Assert.IsTrue(report.Passed, report.ToString());
    }

    [TestMethod]
    [DataRow("<button id='t'>Save<span>d</span></button>", "Saved")]
    [DataRow("<button id='t'><b>Bold</b><i>Italic</i><a>Link</a></button>", "BoldItalicLink")]
    [DataRow("<button id='t'><span>Save</span><div>All</div></button>", "Save All")]
    [DataRow("<button id='t'>Line<br>break</button>", "Line break")]
    [DataRow("<button id='t'>Save<span style='display: block'>All</span></button>", "Save All")]
    [DataRow("<button id='t'>Save<div style='display:inline'>d</div></button>", "Saved")]
    public void Only_separate_boxes_separate_words(string html, string expected)
        => Assert.AreEqual(expected, Name(html));

    [TestMethod]
    [DataRow("<div hidden id='l'><b>Billing</b> address</div><input id='t' aria-labelledby='l'>", "Billing address")]
    [DataRow("<span id='l'>Billing <span hidden>secret</span></span><input id='t' aria-labelledby='l'>", "Billing")]
    [DataRow("<p id='d' hidden>Must be <b>8+</b> chars.</p><input id='t' aria-label='Password' aria-describedby='d'>", "Password")]
    [DataRow("<div aria-hidden='true'><button id='t'><span>Close</span></button></div>", "Close")]
    [DataRow("<div aria-hidden='true'><label for='t'>Email</label><input id='t'></div>", "Email")]
    [DataRow("<div aria-hidden='true'><label>Remember me <input id='t' type='checkbox'></label></div>", "Remember me")]
    [DataRow("<label for='t' hidden>Hidden label</label><input id='t'>", "Hidden label")]
    [DataRow("<details><summary>More</summary><button id='t'>Inside <span style='display:none'>x</span>closed</button></details>", "Inside closed")]
    [DataRow("<div hidden id='l'>Billing <span aria-hidden='true'>x</span></div><input id='t' aria-labelledby='l'>", "Billing")]
    public void A_hidden_starting_node_counts_but_content_hidden_below_it_does_not(string html, string expected)
        => Assert.AreEqual(expected, Name(html));

    [TestMethod]
    public void Hidden_describedby_target_keeps_its_hidden_content()
        => Assert.AreEqual("Must be 8+ chars.", Description("<p id='d' hidden>Must be <b>8+</b> chars.</p><input id='t' aria-label='Password' aria-describedby='d'>"));

    [TestMethod]
    [DataRow("<select aria-label='n'><option>1</option><option selected>2</option><option>3</option></select>", "Ship 2 items")]
    [DataRow("<select aria-label='n'><option disabled>0</option><option>1</option></select>", "Ship 1 items")]
    [DataRow("<select aria-label='n' multiple><option selected>1</option><option>2</option><option selected>3</option></select>", "Ship 1 3 items")]
    [DataRow("<span role='listbox' aria-label='n'><span role='option' aria-selected='false'>1</span><span role='option' aria-selected='true'>2</span></span>", "Ship 2 items")]
    [DataRow("<input role='combobox' aria-expanded='false' aria-label='n' value='4'>", "Ship 4 items")]
    [DataRow("<span role='slider' aria-label='n' aria-valuenow='7' aria-valuetext='seven'></span>", "Ship seven items")]
    [DataRow("<span role='spinbutton' aria-label='n' aria-valuenow='3'></span>", "Ship 3 items")]
    [DataRow("<input type='range' aria-label='n'>", "Ship 50 items")]
    [DataRow("<input type='number' aria-label='n' value='6'>", "Ship 6 items")]
    [DataRow("<progress aria-label='n' value='5' max='10'></progress>", "Ship 5 items")]
    [DataRow("<input aria-label='n' value='9'>", "Ship 9 items")]
    public void Embedded_controls_contribute_their_value(string control, string expected)
        => Assert.AreEqual(expected, Name($"<input id='t' type='checkbox' aria-labelledby='l'><span id='l'>Ship {control} items</span>"));

    [TestMethod]
    [DataRow("<input id='t' type='image' src='go.png' alt='Go'>", "Go")]
    [DataRow("<input id='t' type='image' src='go.png' alt=' ' value='Send'>", "Send")]
    [DataRow("<input id='t' type='image' src='go.png' title='Send it'>", "Send it")]
    [DataRow("<input id='t' type='image' src='go.png'>", "Submit")]
    [DataRow("<input id='t' type='submit'>", "Submit")]
    [DataRow("<input id='t' type='reset' value='Clear'>", "Clear")]
    [DataRow("<input id='t' title='' placeholder='Search'>", "Search")]
    [DataRow("<input id='t' title='  ' placeholder='Search'>", "Search")]
    [DataRow("<input id='t' title='Query' placeholder='Search'>", "Query")]
    [DataRow("<fieldset id='t'><legend>Shipping</legend><input aria-label='Street'></fieldset>", "Shipping")]
    [DataRow("<table id='t'><caption>Prices</caption><tr><td>1</td></tr></table>", "Prices")]
    [DataRow("<select aria-label='Fruit'><optgroup id='t' label='Citrus'><option>Lime</option></optgroup></select>", "Citrus")]
    [DataRow("<label>Name <input id='t'><input id='other'></label>", "Name")]
    [DataRow("<label for='t'>First</label><input id='t'><label for='t'>Second</label>", "First Second")]
    public void Host_language_labels(string html, string expected)
        => Assert.AreEqual(expected, Name(html));

    [TestMethod]
    public void Wrapping_label_names_only_its_first_labelable_descendant()
        => Assert.AreEqual(string.Empty, Body("<label>Name <input><input id='t'></label>").QuerySelector("#t")!.AccessibleName());

    [TestMethod]
    public void Description_skips_a_title_that_supplied_the_name()
    {
        Assert.AreEqual("Save", Name("<button id='t' title='Save'><svg aria-hidden='true'></svg></button>"));
        Assert.AreEqual(string.Empty, Description("<button id='t' title='Save'><svg aria-hidden='true'></svg></button>"));
        Assert.AreEqual("Saves the draft", Description("<button id='t' title='Saves the draft'>Save</button>"));
    }

    [TestMethod]
    public void Description_supports_aria_description()
    {
        Assert.AreEqual("Removes it for good", Description("<button id='t' aria-description='Removes it for good' title='ignored'>Delete</button>"));
        Assert.AreEqual("From the reference", Description("<p id='d'>From the reference</p><button id='t' aria-describedby='d' aria-description='ignored'>Delete</button>"));
    }
}
