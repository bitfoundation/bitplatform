namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Sticky;

public partial class BitStickyDemo
{
    private readonly string example1RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .sticky {
        padding: 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-pri);
    }
</style>

<div class=""vertical-container"">
    <BitSticky Class=""sticky"">Basic Sticky</BitSticky>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
    <p>Imagine this space as a window into the future, empty yet alive with the energy of endless possibilities, ready to transform into something meaningful.</p>
</div>";

    private readonly string example2RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .horizontal-container {
        gap: 1rem;
        display: flex;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        white-space: nowrap;
        align-items: center;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .sticky {
        padding: 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-pri);
    }
</style>

<BitChoiceGroup Horizontal Label=""Vertical"" TItem=""BitChoiceGroupOption<BitStickyPosition>"" TValue=""BitStickyPosition"" @bind-Value=""verticalPosition"">
    <BitChoiceGroupOption Text=""Top"" Value=""BitStickyPosition.Top"" />
    <BitChoiceGroupOption Text=""Bottom"" Value=""BitStickyPosition.Bottom"" />
    <BitChoiceGroupOption Text=""TopAndBottom"" Value=""BitStickyPosition.TopAndBottom"" />
</BitChoiceGroup>
<div class=""vertical-container"">
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <BitSticky Class=""sticky"" Position=""verticalPosition"">Position=""@verticalPosition""</BitSticky>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
    <p>Imagine this space as a window into the future, empty yet alive with the energy of endless possibilities, ready to transform into something meaningful.</p>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
</div>
<br />
<BitChoiceGroup Horizontal Label=""Horizontal"" TItem=""BitChoiceGroupOption<BitStickyPosition>"" TValue=""BitStickyPosition"" @bind-Value=""horizontalPosition"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitStickyPosition.Start"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitStickyPosition.End"" />
    <BitChoiceGroupOption Text=""StartAndEnd"" Value=""BitStickyPosition.StartAndEnd"" />
</BitChoiceGroup>
<div class=""horizontal-container"">
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</p>
    <BitSticky Class=""sticky"" Position=""horizontalPosition"">Position=""@horizontalPosition""</BitSticky>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</p>
</div>";
    private readonly string example2CsharpCode = @"
private BitStickyPosition verticalPosition = BitStickyPosition.TopAndBottom;
private BitStickyPosition horizontalPosition = BitStickyPosition.StartAndEnd;";

    private readonly string example3RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .horizontal-container {
        gap: 1rem;
        display: flex;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        white-space: nowrap;
        align-items: center;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .sticky {
        padding: 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-pri);
    }
</style>

<div class=""vertical-container"">
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <BitSticky Class=""sticky"" Top=""16"" Bottom=""2rem"">Top=""16"" Bottom=""2rem""</BitSticky>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
    <p>Imagine this space as a window into the future, empty yet alive with the energy of endless possibilities, ready to transform into something meaningful.</p>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
</div>
<br />
<div class=""horizontal-container"">
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</p>
    <BitSticky Class=""sticky"" Left=""16"" Right=""2rem"">Left=""16"" Right=""2rem""</BitSticky>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</p>
</div>";

    private readonly string example4RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .sticky {
        padding: 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .stuck-shadow {
        box-shadow: var(--bit-shd-md);
    }
</style>

<div>Stuck: <b>@isStuck</b></div>
<div class=""vertical-container"">
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <BitSticky Class=""sticky"" StuckClass=""stuck-shadow"" StuckStyle=""font-weight: 600"" OnStuckChanged=""v => isStuck = v"">
        @(isStuck ? ""Stuck"" : ""Not stuck yet"")
    </BitSticky>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
    <p>Imagine this space as a window into the future, empty yet alive with the energy of endless possibilities, ready to transform into something meaningful.</p>
</div>";
    private readonly string example4CsharpCode = @"
private bool isStuck;";

    private readonly string example5RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .sticky {
        padding: 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .edge-shadow.bit-stk-stc-top {
        box-shadow: var(--bit-shd-appbar-top);
    }

    .edge-shadow.bit-stk-stc-btm {
        box-shadow: var(--bit-shd-appbar-bottom);
    }
</style>

<div>Pinned to: <b>@stuckEdges</b></div>
<div class=""vertical-container"">
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <BitSticky Class=""sticky edge-shadow"" Position=""BitStickyPosition.TopAndBottom"" OnStuckEdgesChanged=""v => stuckEdges = v"">
        @(stuckEdges is BitStickyEdges.None ? ""Travelling with the content"" : $""Pinned to {stuckEdges}"")
    </BitSticky>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
    <p>Imagine this space as a window into the future, empty yet alive with the energy of endless possibilities, ready to transform into something meaningful.</p>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
</div>";
    private readonly string example5CsharpCode = @"
private BitStickyEdges stuckEdges;";

    private readonly string example6RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .sticky {
        padding: 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .demo-table {
        width: 100%;
        border-spacing: 0;
        border-collapse: separate;
    }

    .demo-table td {
        padding: 0.5rem;
        white-space: nowrap;
    }

    .table-head {
        padding: 0.5rem;
        text-align: start;
        white-space: nowrap;
        background-color: var(--bit-clr-bg-sec);
    }
</style>

<div class=""vertical-container"">
    <BitSticky Element=""header"" Class=""sticky"">A sticky header</BitSticky>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <BitSticky Element=""footer"" Class=""sticky"" Position=""BitStickyPosition.Bottom"">A sticky footer</BitSticky>
</div>
<br />
<div class=""vertical-container"">
    <table class=""demo-table"">
        <thead>
            <tr>
                <BitSticky Element=""th"" Class=""table-head"">Name</BitSticky>
                <BitSticky Element=""th"" Class=""table-head"">Role</BitSticky>
            </tr>
        </thead>
        <tbody>
            @foreach (var person in people)
            {
                <tr>
                    <td>@person.Name</td>
                    <td>@person.Role</td>
                </tr>
            }
        </tbody>
    </table>
</div>";
    private readonly string example6CsharpCode = @"
private record Person(string Name, string Role, string KnownFor, int Born);

private readonly Person[] people =
[
    new(""Ada Lovelace"", ""Mathematician"", ""The first published algorithm"", 1815),
    new(""Grace Hopper"", ""Rear Admiral"", ""The first compiler"", 1906),
    new(""Alan Turing"", ""Cryptanalyst"", ""The Turing machine"", 1912),
    new(""Katherine Johnson"", ""Physicist"", ""Orbital mechanics at NASA"", 1918),
    new(""Barbara Liskov"", ""Computer Scientist"", ""The substitution principle"", 1939),
    new(""Donald Knuth"", ""Author"", ""The Art of Computer Programming"", 1938),
    new(""Edsger Dijkstra"", ""Computer Scientist"", ""The shortest-path algorithm"", 1930),
    new(""Margaret Hamilton"", ""Software Engineer"", ""The Apollo flight software"", 1936),
];";

    private readonly string example7RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .sticky {
        padding: 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .positioned-box {
        z-index: 2;
        padding: 0.5rem;
        position: relative;
        color: var(--bit-clr-pri-text);
        background-color: var(--bit-clr-pri);
    }
</style>

<div class=""vertical-container"">
    <BitSticky Class=""sticky"">Default z-index</BitSticky>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <div class=""positioned-box"">A positioned box with z-index: 2</div>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
</div>
<br />
<div class=""vertical-container"">
    <BitSticky Class=""sticky"" ZIndex=""3"">ZIndex=""3""</BitSticky>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <div class=""positioned-box"">A positioned box with z-index: 2</div>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
</div>";

    private readonly string example8RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .table-container {
        height: 12rem;
        overflow: auto;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .bar {
        padding: 0.5rem;
        font-weight: 600;
    }

    .demo-table {
        width: 100%;
        border-spacing: 0;
        border-collapse: separate;
    }

    .demo-table td {
        padding: 0.5rem;
        white-space: nowrap;
    }

    .wide-table {
        min-width: 40rem;
    }

    .table-head {
        padding: 0.5rem;
        text-align: start;
        white-space: nowrap;
        background-color: var(--bit-clr-bg-sec);
    }

    .row-head {
        padding: 0.5rem;
        text-align: start;
        white-space: nowrap;
        font-weight: normal;
        background-color: var(--bit-clr-bg-pri);
    }
</style>

<div class=""vertical-container"">
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <BitSticky ElevateOnStuck Class=""bar"" Position=""BitStickyPosition.TopAndBottom"">Elevated only while stuck</BitSticky>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
    <p>Imagine this space as a window into the future, empty yet alive with the energy of endless possibilities, ready to transform into something meaningful.</p>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
</div>
<br />
<div class=""table-container"">
    <table class=""demo-table wide-table"">
        <thead>
            <tr>
                <BitSticky Element=""th"" ElevateOnStuck Top=""0"" Left=""0"" ZIndex=""2"" Class=""table-head"">Name</BitSticky>
                <BitSticky Element=""th"" ElevateOnStuck Class=""table-head"">Role</BitSticky>
                <BitSticky Element=""th"" ElevateOnStuck Class=""table-head"">Known for</BitSticky>
                <BitSticky Element=""th"" ElevateOnStuck Class=""table-head"">Born</BitSticky>
            </tr>
        </thead>
        <tbody>
            @foreach (var person in people)
            {
                <tr>
                    <BitSticky Element=""th"" ElevateOnStuck Position=""BitStickyPosition.Start"" Class=""row-head"" scope=""row"">@person.Name</BitSticky>
                    <td>@person.Role</td>
                    <td>@person.KnownFor</td>
                    <td>@person.Born</td>
                </tr>
            }
        </tbody>
    </table>
</div>";
    private readonly string example8CsharpCode = @"
private record Person(string Name, string Role, string KnownFor, int Born);

private readonly Person[] people =
[
    new(""Ada Lovelace"", ""Mathematician"", ""The first published algorithm"", 1815),
    new(""Grace Hopper"", ""Rear Admiral"", ""The first compiler"", 1906),
    new(""Alan Turing"", ""Cryptanalyst"", ""The Turing machine"", 1912),
    new(""Katherine Johnson"", ""Physicist"", ""Orbital mechanics at NASA"", 1918),
    new(""Barbara Liskov"", ""Computer Scientist"", ""The substitution principle"", 1939),
    new(""Donald Knuth"", ""Author"", ""The Art of Computer Programming"", 1938),
    new(""Edsger Dijkstra"", ""Computer Scientist"", ""The shortest-path algorithm"", 1930),
    new(""Margaret Hamilton"", ""Software Engineer"", ""The Apollo flight software"", 1936),
];";

    private readonly string example9RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .sticky {
        padding: 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .link-list {
        gap: 0.5rem;
        display: flex;
        padding: 0.5rem 0;
        align-items: start;
        flex-direction: column;
    }
</style>

<BitToggle @bind-Value=""reservesScrollPadding"" Text=""ScrollPadding"" />
<div class=""vertical-container"">
    <BitSticky Class=""sticky"" AriaLabel=""Reading list"" ScrollPadding=""reservesScrollPadding"">Reading list</BitSticky>
    <div class=""link-list"">
        @foreach (var person in people)
        {
            <BitLink Href=""@($""https://en.wikipedia.org/wiki/{person.Name.Replace(' ', '_')}"")"">@person.Name</BitLink>
        }
    </div>
</div>";
    private readonly string example9CsharpCode = @"
private bool reservesScrollPadding = true;

private record Person(string Name, string Role, string KnownFor, int Born);

private readonly Person[] people =
[
    new(""Ada Lovelace"", ""Mathematician"", ""The first published algorithm"", 1815),
    new(""Grace Hopper"", ""Rear Admiral"", ""The first compiler"", 1906),
    new(""Alan Turing"", ""Cryptanalyst"", ""The Turing machine"", 1912),
    new(""Katherine Johnson"", ""Physicist"", ""Orbital mechanics at NASA"", 1918),
    new(""Barbara Liskov"", ""Computer Scientist"", ""The substitution principle"", 1939),
    new(""Donald Knuth"", ""Author"", ""The Art of Computer Programming"", 1938),
    new(""Edsger Dijkstra"", ""Computer Scientist"", ""The shortest-path algorithm"", 1930),
    new(""Margaret Hamilton"", ""Software Engineer"", ""The Apollo flight software"", 1936),
];";

    private readonly string example10RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .sticky {
        padding: 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-pri);
    }
</style>

<BitToggle @bind-Value=""isStickyEnabled"" Text=""Sticky enabled"" />
<div class=""vertical-container"">
    <BitSticky Class=""sticky"" IsEnabled=""isStickyEnabled"">
        @(isStickyEnabled ? ""Sticking to the top"" : ""Scrolling away with the content"")
    </BitSticky>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
</div>";
    private readonly string example10CsharpCode = @"
private bool isStickyEnabled = true;";

    private readonly string example11RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .bar {
        padding: 0.5rem;
        font-weight: 600;
    }
</style>

<div class=""vertical-container""
     style=""--bit-Sticky-offset-top: 0.5rem; --bit-Sticky-background: var(--bit-clr-bg-sec); --bit-Sticky-shadow-top: 0 6px 12px -4px var(--bit-clr-pri);"">
    <BitSticky ElevateOnStuck Class=""bar"">Styled by the container</BitSticky>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <BitSticky ElevateOnStuck Class=""bar"">The next section takes over</BitSticky>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
    <p>Imagine this space as a window into the future, empty yet alive with the energy of endless possibilities, ready to transform into something meaningful.</p>
</div>";

    private readonly string example12RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .sticky {
        padding: 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .bar {
        padding: 0.5rem;
        font-weight: 600;
    }
</style>

<BitParams Parameters=""stickyParams"">
    <div class=""vertical-container"">
        <BitSticky Class=""bar"">Cascaded: elevated, 0.5rem from the top</BitSticky>
        <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
        <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
        <BitSticky Class=""bar"">Cascaded as well</BitSticky>
        <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
        <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
        <BitSticky Class=""sticky"" ElevateOnStuck=""false"">Its own ElevateOnStuck, the cascaded rest</BitSticky>
        <p>Imagine this space as a window into the future, empty yet alive with the energy of endless possibilities, ready to transform into something meaningful.</p>
        <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
        <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    </div>
</BitParams>";
    private readonly string example12CsharpCode = @"
private readonly BitStickyParams[] stickyParams =
[
    new() { ElevateOnStuck = true, Top = ""0.5rem"" }
];";

    private readonly string example13RazorCode = @"
<style>
    .vertical-container {
        height: 12rem;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .custom-class {
        padding: 0.5rem;
        border-radius: 0.5rem;
        color: var(--bit-clr-pri-text);
        background-color: var(--bit-clr-pri);
        border: 2px dashed var(--bit-clr-brd-pri);
    }
</style>

<div class=""vertical-container"">
    <BitSticky Style=""padding: 0.5rem; border-radius: 0.5rem; color: var(--bit-clr-sec-text); background-color: var(--bit-clr-sec);"">
        Styled Sticky
    </BitSticky>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
</div>
<br />
<div class=""vertical-container"">
    <BitSticky Class=""custom-class"">Classed Sticky</BitSticky>
    <p>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. Each word carried meaning, each pause brought understanding.</p>
    <p>Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. These placeholder words symbolize the beginning of something remarkable.</p>
    <p>In the beginning, there is silence: a blank canvas yearning to be filled, a quiet space where creativity waits to awaken, standing in place of ideas yet to come.</p>
    <p>In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and each word can become something extraordinary.</p>
</div>";

    private readonly string example14RazorCode = @"
<style>
    .horizontal-container {
        gap: 1rem;
        display: flex;
        overflow: auto;
        padding: 0.5rem;
        max-width: 32rem;
        white-space: nowrap;
        align-items: center;
        border: 1px solid var(--bit-clr-brd-pri);
    }

    .sticky {
        padding: 0.5rem;
        color: var(--bit-clr-fg-pri);
        background-color: var(--bit-clr-bg-sec);
        border: 1px solid var(--bit-clr-brd-pri);
    }
</style>

<div dir=""rtl"">
    <div class=""horizontal-container"">
        <p>روزی روزگاری، داستان‌ها میان مردم پیوند می‌ساختند؛ هم‌نوایی صداهایی که رویاهای مشترک می‌آفریدند.</p>
        <BitSticky Dir=""BitDir.Rtl"" Class=""sticky"" Position=""BitStickyPosition.Start"">چسبیده به آغاز</BitSticky>
        <p>روزی روزگاری، داستان‌ها میان مردم پیوند می‌ساختند؛ هم‌نوایی صداهایی که رویاهای مشترک می‌آفریدند.</p>
    </div>
    <br />
    <div class=""horizontal-container"">
        <p>روزی روزگاری، داستان‌ها میان مردم پیوند می‌ساختند؛ هم‌نوایی صداهایی که رویاهای مشترک می‌آفریدند.</p>
        <BitSticky Dir=""BitDir.Rtl"" Class=""sticky"" Position=""BitStickyPosition.End"">چسبیده به پایان</BitSticky>
        <p>روزی روزگاری، داستان‌ها میان مردم پیوند می‌ساختند؛ هم‌نوایی صداهایی که رویاهای مشترک می‌آفریدند.</p>
    </div>
</div>";
}
