namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// How an agent's reply is announced: the tone and the vibration, the sound toggle, and the notification the Notify me switch turns
/// on. A test can't hear or see these, so the browser's own calls for them are counted.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ReplyAlertsUITests : ReviewPageTest
{
    private const string AgentName = "Claude Code";

    /// <summary>Counts what the page asks the browser to announce; each test sets what the browser answers to asking for notifications.</summary>
    private const string CountAlerts = """
        (() => {
            const alerts = window.__alerts = { tones: 0, vibrations: 0, notifications: [], asked: 0 };
            const createOscillator = AudioContext.prototype.createOscillator;

            AudioContext.prototype.createOscillator = function () {
                alerts.tones++;
                return createOscillator.call(this);
            };

            navigator.vibrate = () => {
                alerts.vibrations++;
                return true;
            };

            const answer = 'ANSWER';

            window.Notification = class {
                static permission = answer === 'granted' || answer === 'denied' ? answer : 'default';

                static async requestPermission() {
                    alerts.asked++;
                    window.Notification.permission = answer;
                    return answer;
                }

                constructor(title, options) {
                    alerts.notifications.push({ title, body: options?.body, tag: options?.tag, data: options?.data, silent: options?.silent === true });
                }
            };
        })();
        """;

    private sealed record Alerts(int Tones, int Vibrations, int Asked, Notified[] Notifications);

    private sealed record Notified(string Title, string? Body, string? Tag, string? Data, bool Silent);

    private ILocator SoundButton => Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.MuteReplySound, Exact = true });

    private ILocator NotifySwitch => Tools.GetByRole(AriaRole.Switch, new() { Name = WorkbenchStrings.ReplyNotifications });

    /// <summary>Starts every page with the alerts counted and the browser answering so when it's asked for notifications.</summary>
    private async Task CountAlertsAnswering(string answer)
    {
        await Context.AddInitScriptAsync(CountAlerts.Replace("ANSWER", answer, StringComparison.Ordinal));
    }

    private async Task<Alerts> ReadAlerts() => System.Text.Json.JsonSerializer.Deserialize<Alerts>(await Page.EvaluateAsync<string>("() => JSON.stringify(window.__alerts)"), System.Text.Json.JsonSerializerOptions.Web)!;

    private Task<Alerts> WaitForAlerts(Func<Alerts, bool> condition) => WorkbenchTestReviewer.WaitFor(ReadAlerts, condition);

    /// <summary>Opens a page and waits until it hears the review, with a comment of its own to answer.</summary>
    private async Task<string> OpenACommentOn(string pageKey)
    {
        var text = Unique("Waiting for a reply");
        var id = await SeedComment(pageKey, text);

        await OpenWorkbench(pageKey);
        await OpenReview();

        await Expect(ThreadWith(text)).ToBeVisibleAsync();

        return id;
    }

    [TestMethod, WithAnAgent]
    public async Task AnAgentsReply_Should_PlayAToneAndVibrateOnce(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("default");

        var id = await OpenACommentOn("shifts");

        await Server.Agent.Reply(AgentName, id, Unique("An answer"), CancellationToken.None);

        var alerts = await WaitForAlerts(a => a.Tones > 0 && a.Vibrations > 0);

        Assert.AreEqual(1, alerts.Tones);
        Assert.AreEqual(1, alerts.Vibrations);
        Assert.IsEmpty(alerts.Notifications, "Notifications are off until they are turned on.");
    }

    [TestMethod, WithAnAgent]
    public async Task AReviewersReply_Should_NotBeAnnounced(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("default");

        var text = Unique("Waiting for a reply");
        var id = await SeedComment("shifts", text);

        await OpenWorkbench("shifts");
        await OpenReview();
        await OpenThread(text);

        var reply = Unique("From a reviewer");

        await ReplyBox.FillAsync(reply);
        await ReplyBox.PressAsync("Control+Enter");

        await Expect(ThreadWith(text)).ToContainTextAsync(reply);

        var marker = Unique("From the agent");

        await Server.Agent.Reply(AgentName, id, marker, CancellationToken.None);

        await Expect(ThreadWith(text)).ToContainTextAsync(marker);

        Assert.AreEqual(1, (await WaitForAlerts(a => a.Tones > 0)).Tones, "Only the agent's reply played the tone.");
    }

    [TestMethod, WithAnAgent]
    public async Task TheSoundToggle_Should_MuteTheToneAndStayAfterAReload(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("default");

        var id = await OpenACommentOn("shifts");

        await Expect(SoundButton).ToHaveAttributeAsync("aria-pressed", "false");

        await Press(SoundButton);

        await Expect(SoundButton).ToHaveAttributeAsync("aria-pressed", "true");
        await CloseTools();

        await Server.Agent.Reply(AgentName, id, Unique("Heard by nobody"), CancellationToken.None);

        var alerts = await WaitForAlerts(a => a.Vibrations > 0);

        Assert.AreEqual(0, alerts.Tones);

        await Page.ReloadAsync();
        await WaitForViews();
        await OpenTools();

        await Expect(SoundButton).ToHaveAttributeAsync("aria-pressed", "true");

        await Press(SoundButton);

        await Expect(SoundButton).ToHaveAttributeAsync("aria-pressed", "false");
        await CloseTools();

        await Server.Agent.Reply(AgentName, id, Unique("Heard again"), CancellationToken.None);

        await WaitForAlerts(a => a.Tones > 0);
    }

    [TestMethod, WithAnAgent]
    public async Task NotifyMe_Should_ShowANotificationForTheAgentsReply(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("granted");

        var id = await OpenACommentOn("shifts");

        await Expect(NotifySwitch).Not.ToBeCheckedAsync();

        await Press(NotifySwitch);

        await Expect(NotifySwitch).ToBeCheckedAsync();

        var worker = await WorkbenchTestReviewer.WaitFor(() => Page.EvaluateAsync<string?>("async () => (await navigator.serviceWorker.getRegistration())?.active?.scriptURL ?? null"), url => url is not null);

        Assert.EndsWith("/workbench-sw.js", worker!);

        var answer = Unique("An answer");

        await Server.Agent.Reply(AgentName, id, answer, CancellationToken.None);

        var notified = (await WaitForAlerts(a => a.Notifications.Length > 0)).Notifications.Single();

        Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.AgentRepliedOn, AgentName, WorkbenchStrings.ShiftsTitle), notified.Title);
        Assert.AreEqual(answer, notified.Body);
        Assert.AreEqual(id, notified.Tag);
        Assert.AreEqual("/workbench/shifts", notified.Data);
        Assert.IsFalse(notified.Silent);
    }

    [TestMethod, WithAnAgent]
    public async Task AMutedNotification_Should_BeSilent(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("granted");

        var id = await OpenACommentOn("shifts");

        await Press(NotifySwitch);
        await Expect(NotifySwitch).ToBeCheckedAsync();

        await Press(SoundButton);
        await Expect(SoundButton).ToHaveAttributeAsync("aria-pressed", "true");

        await Server.Agent.Reply(AgentName, id, Unique("Quietly"), CancellationToken.None);

        var alerts = await WaitForAlerts(a => a.Notifications.Length > 0);

        Assert.IsTrue(alerts.Notifications.Single().Silent);
        Assert.AreEqual(0, alerts.Tones);
    }

    [TestMethod, WithAnAgent]
    public async Task NotifyMe_Should_StayAfterAReloadAndTurnOff(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("granted");

        var id = await OpenACommentOn("shifts");

        await Press(NotifySwitch);
        await Expect(NotifySwitch).ToBeCheckedAsync();

        await Page.ReloadAsync();
        await WaitForViews();
        await OpenReview();

        await Expect(NotifySwitch).ToBeCheckedAsync();

        await Press(NotifySwitch);
        await Expect(NotifySwitch).Not.ToBeCheckedAsync();

        await Server.Agent.Reply(AgentName, id, Unique("Not shown"), CancellationToken.None);

        var alerts = await WaitForAlerts(a => a.Tones > 0);

        Assert.IsEmpty(alerts.Notifications);
    }

    [TestMethod, WithAnAgent]
    public async Task NotifyMe_Should_SaySoWhenTheBrowserRefuses(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("denied");

        await OpenACommentOn("shifts");

        await Expect(Tools.GetByText(WorkbenchStrings.NotificationsBlocked)).ToHaveCountAsync(0);

        await Press(NotifySwitch);

        await Expect(Tools.GetByText(WorkbenchStrings.NotificationsBlocked)).ToBeVisibleAsync();
        await Expect(NotifySwitch).Not.ToBeCheckedAsync();
    }

    [TestMethod, WithAnAgent]
    public async Task TheFirstComment_Should_OfferNotificationsOnce(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("granted");
        await ClearComments("shifts");

        await OpenWorkbench("shifts");

        await LeaveAComment(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading }), Unique("My first"));

        await WaitForAlerts(a => a.Asked == 1);
        await Expect(NotifySwitch).ToBeCheckedAsync();

        await Press(NotifySwitch);
        await Expect(NotifySwitch).Not.ToBeCheckedAsync();

        await CloseTools();

        await LeaveAComment(View().Locator(".filters"), Unique("My second"));

        await Expect(NotifySwitch).Not.ToBeCheckedAsync();

        Assert.AreEqual(1, (await ReadAlerts()).Asked);
    }

    [TestMethod, WithAnAgent]
    public async Task AFirstComment_Should_AskNothingWhenTheBrowserAlreadyRefused(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("denied");
        await ClearComments("shifts");

        await OpenWorkbench("shifts");

        await LeaveAComment(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading }), Unique("My first"));

        await Expect(NotifySwitch).Not.ToBeCheckedAsync();
        await Expect(Tools.GetByText(WorkbenchStrings.NotificationsBlocked)).ToHaveCountAsync(0);

        Assert.AreEqual(0, (await ReadAlerts()).Asked);
    }

    [TestMethod, WithAnAgent]
    public async Task AReload_Should_NotAnnounceOlderReplies(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("granted");

        var id = await OpenACommentOn("shifts");

        await Server.Agent.Reply(AgentName, id, Unique("Already heard"), CancellationToken.None);

        await WaitForAlerts(a => a.Tones == 1);

        await Page.ReloadAsync();
        await WaitForViews();
        await OpenReview();

        var answer = Unique("Heard after the reload");

        await Server.Agent.Reply(AgentName, id, answer, CancellationToken.None);

        await Expect(Tools.Locator(".message.from-agent").Filter(new() { HasText = answer })).ToBeVisibleAsync();

        Assert.AreEqual(1, (await WaitForAlerts(a => a.Tones > 0)).Tones, "Only the reply after the reload played the tone.");
    }

    [TestMethod, WithAnAgent]
    public async Task TheFlowsPage_Should_AnnounceRepliesAndOfferTheSoundToggleToo(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("default");

        var id = await SeedComment("shifts", Unique("Waiting on another page"));

        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flows).ToString());

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowPos })).ToBeVisibleAsync();
        await Expect(SoundButton).ToBeVisibleAsync();

        await Server.Agent.Reply(AgentName, id, Unique("An answer"), CancellationToken.None);

        await WaitForAlerts(a => a.Tones == 1 && a.Vibrations == 1);
    }

    [TestMethod, WithoutAnAgent]
    public async Task WithoutAnAgent_Should_BeNoAlertsNorAWayToSetThem(WorkbenchSurface surface)
    {
        await CountAlertsAnswering("granted");

        var id = await OpenACommentOn("shifts");

        await Expect(NotifySwitch).ToHaveCountAsync(0);
        await Expect(SoundButton).ToHaveCountAsync(0);

        var answer = Unique("Read later");

        await Server.Agent.Reply(AgentName, id, answer, CancellationToken.None);

        await Expect(Tools.Locator(".message.from-agent").Filter(new() { HasText = answer })).ToBeVisibleAsync();

        var alerts = await ReadAlerts();

        Assert.AreEqual(0, alerts.Tones);
        Assert.AreEqual(0, alerts.Vibrations);
        Assert.IsEmpty(alerts.Notifications);

        await Page.GotoAsync(new Uri(Server.Address, WorkbenchUrls.Flows).ToString());

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = WorkbenchStrings.SampleFlowPos })).ToBeVisibleAsync();
        await Expect(SoundButton).ToHaveCountAsync(0);
    }
}
