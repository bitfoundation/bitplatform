using Spectre.Console;
using Spectre.Console.Rendering;

namespace Bit.Cli.Infrastructure;

public sealed class AgentIdeas
{
    public const int Rows = 7;

    private const string AgentColor = "#C084FC";
    private const double HoldSeconds = 7;
    private const double GapSeconds = 0.6;

    internal static IReadOnlyList<AgentIdea> All { get; } =
    [
        new("Run", "#A3E635",
        [
            new YouSay("Run the web app"),
            new AgentWorks("starting the web app", "the app is running", 2.4),
            new AgentSays("Open calm-river-42.trycloudflare.com on any device you like"),
            new YouSay("Now run the Android app on my mobile"),
            new AgentWorks("building it for Android and installing it on your phone", "it's open on your phone", 2.6),
            new YouSay("Bring up the Workbench, I want to give feedback on the UI"),
            new AgentWorks("starting the Workbench", "the Workbench is open in your browser", 1.8),
            new AgentSays("Pin a comment on anything you want changed, I'm listening"),
            new YouSay("Now run the landing page"),
            new AgentWorks("starting the landing page", "the landing page is open in your browser", 1.8),
        ]),
        new("Rebrand", "#E879F9",
        [
            new YouSay("Give my app a look inspired by grok.com, with my own logo"),
            new AgentWorks("reading grok.com's colors, fonts, spacing and motion", "read grok.com's colors, fonts, spacing and motion", 2.4),
            new AgentWorks("making the theme, the home page and the menu", "new theme in light and dark, home page and menu too", 2.2),
            new AgentWorks("checking every state: focus, errors, phones, right to left", "checked every state, contrast measured", 2.0),
            new AgentSays("It's open in your browser. Anything to change?"),
            new IdeaNote("This is how bitfabric.co got its look"),
        ]),
        new("Port", "#38BDF8",
        [
            new YouSay("Port my old shop in ./legacy into this project, same features"),
            new AgentWorks("reading ./legacy", "read ./legacy: 18 screens, 9 tables, 4 reports", 2.4),
            new AgentSays("I suggest a shop app for customers and an admin app for staff"),
            new AgentSays("The plan has 24 small steps, screen by screen. Shall I start?"),
            new YouSay("Yes, start"),
            new AgentWorks("building the Products screen", "Products works like the old one, shown side by side", 2.2),
            new AgentWorks("building the other 17 screens", "all 18 screens work like the old ones", 2.4),
            new AgentSays("Want me to bring your old data over too?"),
        ]),
        new("Deploy", "#60A5FA",
        [
            new YouSay("Deploy it to my server, ssh root@203.0.113.7, on shop.example.com"),
            new AgentWorks("setting up the server: Docker, a GitHub runner, key-only SSH", "server ready: Docker, a GitHub runner, key-only SSH", 2.4),
            new AgentSays("One thing I need from you: a Cloudflare token for a few days"),
            new AgentSays("Here's which permissions to give it, step by step"),
            new YouSay("Done, here it is"),
            new AgentWorks("deploying Test from any branch and Production from main", "Test and Production are live on shop.example.com", 2.6),
            new AgentSays("Your dashboard, with logs and traces: dashboard.shop.example.com"),
            new AgentSays("You can delete the Cloudflare token now", Done: true),
        ]),
        new("Feature", "#34D399",
        [
            new YouSay("Add a bookings page where customers pick a time slot"),
            new AgentSays("A few questions first: how long is a slot? Can people cancel?"),
            new AgentSays("And the reminder: email, SMS or both?"),
            new YouSay("30 minutes, cancel up to a day before, email"),
            new AgentSays("Here's the plan: 7 small steps, in order. Shall I go ahead?"),
            new YouSay("Go ahead"),
            new AgentWorks("designing the page in the Workbench", "designed: every device, every state, light and dark", 2.2),
            new AgentSays("It's open in your browser. What do you think?"),
            new YouSay("Looks great, approved"),
            new AgentWorks("building it into your app", "built into your app", 2.0),
            new AgentWorks("testing it like a real user, on the web and on phones", "tests pass on the web and on phones", 2.2),
            new AgentWorks("reviewing its own work against the project's rules", "reviewed against the project's rules", 1.6),
            new AgentSays("Ready to try on your Test site", Done: true),
        ]),
        new("Fix", "#F59E0B",
        [
            new YouSay("Checkout fails since this morning. Find out why and fix it"),
            new AgentWorks("reading Production's errors, logs and traces", "read Production's errors, logs and traces", 2.4),
            new AgentSays("Found it: the shipping company changed its rates API today"),
            new AgentSays("Their rates now come in a new format our code doesn't read"),
            new AgentWorks("writing a test with their new answer", "the test fails the same way checkout did", 1.8),
            new AgentWorks("updating the code to their new format", "fixed, and the test passes now", 1.6),
            new AgentSays("Fixed on your Test site, ready to go to Production", Done: true),
        ]),
    ];

    internal static double CycleSeconds { get; } = All.Sum(Length);

    public TimeSpan Shown { get; private set; }

    public void Advance(TimeSpan elapsed) => Shown += elapsed;

    public IRenderable Render(int width, string spinnerFrame) => Render(Shown.TotalSeconds, width, spinnerFrame);

    internal static IRenderable Render(double seconds, int width, string spinnerFrame)
    {
        var panelWidth = Math.Min(width - 2, 96);
        var textWidth = panelWidth - 11;
        var (idea, beats) = At(seconds);
        var lines = new List<(string Speaker, string Text)>();

        string Fit(string text, int prefix = 0) => Markup.Escape(text.Length <= textWidth - prefix ? text : text[..(textWidth - prefix - 1)] + "…");

        foreach (var (beat, local) in beats)
        {
            switch (beat)
            {
                case YouSay you:
                    var typed = you.Times.Count(x => x <= local);
                    lines.Add(("you", Fit(you.Text[..typed], 1) + (local < you.TypingSeconds + 0.25 ? "[white]▌[/]" : "")));
                    break;
                case AgentSays says:
                    var shown = local < AgentSays.ThinkSeconds ? -1 : Math.Min(says.Text.Length, (int)((local - AgentSays.ThinkSeconds) / AgentSays.CharSeconds));
                    lines.Add(("agent", shown < 0
                        ? $"[grey]{spinnerFrame}[/]"
                        : says.Done ? $"[green]✓[/] [bold]{Fit(says.Text[..shown], 2)}[/]" : Fit(says.Text[..shown])));
                    break;
                case AgentWorks works:
                    lines.Add(("agent", local < works.WorkSeconds
                        ? $"[grey]{spinnerFrame} {Fit(works.Doing, 2)}[/]"
                        : $"[green]✓[/] {Fit(works.Done, 2)}"));
                    break;
                case IdeaNote note:
                    lines.Add(("", $"[grey italic]{Fit(note.Text)}[/]"));
                    break;
            }
        }

        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap().Width(5).PadRight(2))
            .AddColumn(new GridColumn().NoWrap());

        var skipped = Math.Max(0, lines.Count - Rows);

        for (var i = lines.Count - skipped; i < Rows; i++)
        {
            grid.AddRow(new Text(""), new Text(""));
        }

        var previous = skipped > 0 ? lines[skipped - 1].Speaker : null;

        foreach (var (speaker, text) in lines.Skip(skipped))
        {
            var label = speaker == previous || speaker == "" ? "" : speaker == "you" ? $"[{idea.Color}]you[/]" : $"[{AgentColor}]agent[/]";
            grid.AddRow(new Markup(label), new Markup(text));
            previous = speaker;
        }

        var panel = new Panel(grid)
            .Header($"[grey] ✦ what you can ask your AI agent next · [/][{idea.Color}]{idea.Kind} [/]")
            .RoundedBorder()
            .BorderColor(Color.Grey35)
            .Padding(1, 0, 1, 0);

        panel.Width = panelWidth;
        return new Padder(panel, new Padding(2, 0, 0, 0));
    }

    private static double Length(AgentIdea idea) => idea.Beats.Sum(b => b.Seconds) + HoldSeconds + GapSeconds;

    private static (AgentIdea Idea, List<(IdeaBeat Beat, double Local)> Beats) At(double seconds)
    {
        seconds %= CycleSeconds;

        foreach (var idea in All)
        {
            var length = Length(idea);

            if (seconds >= length)
            {
                seconds -= length;
                continue;
            }

            var started = new List<(IdeaBeat, double)>();

            if (seconds >= length - GapSeconds)
                return (idea, started);

            foreach (var beat in idea.Beats)
            {
                if (seconds < 0)
                    break;

                started.Add((beat, seconds));
                seconds -= beat.Seconds;
            }

            return (idea, started);
        }

        return (All[0], []);
    }
}

internal sealed record AgentIdea(string Kind, string Color, IReadOnlyList<IdeaBeat> Beats);

internal abstract record IdeaBeat
{
    public abstract double Seconds { get; }
}

internal sealed record YouSay : IdeaBeat
{
    public YouSay(string text)
    {
        var random = new Random(text.Length);
        var at = 0.0;

        Text = text;
        Times = new double[text.Length];

        for (var i = 0; i < text.Length; i++)
        {
            Times[i] = at;
            at += 0.03 + random.NextDouble() * 0.045 + text[i] switch { ',' or ':' => 0.2, '.' or '?' => 0.3, ' ' => random.NextDouble() * 0.05, _ => 0 };
        }

        TypingSeconds = at;
    }

    public string Text { get; }

    public double[] Times { get; }

    public double TypingSeconds { get; }

    public override double Seconds => TypingSeconds + 0.6;
}

internal sealed record AgentSays(string Text, bool Done = false) : IdeaBeat
{
    public const double ThinkSeconds = 0.7;

    public const double CharSeconds = 0.012;

    public override double Seconds => ThinkSeconds + Text.Length * (CharSeconds + 0.02) + 0.6;
}

internal sealed record AgentWorks(string Doing, string Done, double WorkSeconds) : IdeaBeat
{
    public override double Seconds => WorkSeconds + 0.5;
}

internal sealed record IdeaNote(string Text) : IdeaBeat
{
    public override double Seconds => 0.2;
}
