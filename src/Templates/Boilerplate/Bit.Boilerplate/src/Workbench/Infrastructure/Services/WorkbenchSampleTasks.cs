namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// The three tasks the Progress page shows while no agent has started one: a first deployment that waits for the user, a
/// new feature being built and a fix that is done. They show what an agent can put there, in the Workbench's language,
/// with times counted back from now.
/// </summary>
public static class WorkbenchSampleTasks
{
    public const string DeployId = "sample-deploy";
    public const string FeatureId = "sample-feature";
    public const string FixId = "sample-fix";

    public static WorkbenchProgressTask[] Create(IStringLocalizer<WorkbenchStrings> localizer)
    {
        var now = DateTimeOffset.UtcNow;
        var you = localizer[nameof(WorkbenchStrings.ProgressYou)].Value;
        string Text(string key) => localizer[key].Value;
        WorkbenchProgressStep Step(string id, string key, WorkbenchProgressStepStatus status, WorkbenchProgressOwner owner = WorkbenchProgressOwner.Agent, string? detail = null, string? link = null) => new(id, Text(key), owner, status, detail, link);
        WorkbenchProgressEntry Update(string key, TimeSpan ago, WorkbenchProgressNoteKind? tone = null) => new(WorkbenchProgressEntryKind.Update, string.Empty, Text(key), now - ago, 0, null) { Tone = tone };
        WorkbenchProgressEntry FromYou(WorkbenchProgressEntryKind kind, string text, TimeSpan ago, long sequence, string? refId = null) => new(kind, you, text, now - ago, sequence, refId);

        var designPage = WorkbenchPageCatalog.Find("pos-dashboard") ?? WorkbenchPageCatalog.Pages.FirstOrDefault();

        string[] emailOptions = [Text(nameof(WorkbenchStrings.SampleDeployAskEmailOption1)), Text(nameof(WorkbenchStrings.SampleDeployAskEmailOption2)), Text(nameof(WorkbenchStrings.SampleDeployAskEmailOption3))];
        string[] orderOptions = [Text(nameof(WorkbenchStrings.SampleFeatureAskOrdersOption1)), Text(nameof(WorkbenchStrings.SampleFeatureAskOrdersOption2)), Text(nameof(WorkbenchStrings.SampleFeatureAskOrdersOption3))];

        var deploy = new WorkbenchProgressTask(DeployId,
            Text(nameof(WorkbenchStrings.SampleDeployTitle)),
            Text(nameof(WorkbenchStrings.SampleDeploySummary)),
            "Claude Code",
            WorkbenchProgressStatus.Working,
            now - TimeSpan.FromMinutes(45),
            now - TimeSpan.FromMinutes(2),
            now - TimeSpan.FromSeconds(20),
            [
                Step("services", nameof(WorkbenchStrings.SampleDeployStepServices), WorkbenchProgressStepStatus.Done),
                Step("environments", nameof(WorkbenchStrings.SampleDeployStepEnvironments), WorkbenchProgressStepStatus.Done),
                Step("server", nameof(WorkbenchStrings.SampleDeployStepServer), WorkbenchProgressStepStatus.Done, WorkbenchProgressOwner.You),
                Step("token", nameof(WorkbenchStrings.SampleDeployStepToken), WorkbenchProgressStepStatus.Doing, WorkbenchProgressOwner.You, Text(nameof(WorkbenchStrings.SampleDeployStepTokenDetail)), "https://dash.cloudflare.com/profile/api-tokens"),
                Step("runner", nameof(WorkbenchStrings.SampleDeployStepRunner), WorkbenchProgressStepStatus.Todo),
                Step("tunnel", nameof(WorkbenchStrings.SampleDeployStepTunnel), WorkbenchProgressStepStatus.Todo),
                Step("first", nameof(WorkbenchStrings.SampleDeployStepFirst), WorkbenchProgressStepStatus.Todo)
            ],
            [
                new("secrets", WorkbenchProgressNoteKind.Warning, Text(nameof(WorkbenchStrings.SampleDeployNoteSecrets))),
                new("one-server", WorkbenchProgressNoteKind.Decision, Text(nameof(WorkbenchStrings.SampleDeployNoteOneServer)))
            ],
            [new(Text(nameof(WorkbenchStrings.SampleDeployLinkCloudflare)), "https://dash.cloudflare.com")],
            [
                new("token", Text(nameof(WorkbenchStrings.SampleDeployAskToken)), null, WorkbenchProgressAskKind.Secret, [], null, now - TimeSpan.FromMinutes(4), null, false)
                {
                    Secret = new(WorkbenchProgressSecretTo.File, null, null)
                },
                new("email", Text(nameof(WorkbenchStrings.SampleDeployAskEmail)), Text(nameof(WorkbenchStrings.SampleDeployAskEmailDetail)), WorkbenchProgressAskKind.Choice, emailOptions, null, now - TimeSpan.FromMinutes(3), null, false),
                new("ssh", Text(nameof(WorkbenchStrings.SampleDeployAskSsh)), Text(nameof(WorkbenchStrings.SampleDeployAskSshDetail)), WorkbenchProgressAskKind.Confirm, [], null, now - TimeSpan.FromMinutes(2), null, false)
            ],
            [
                Update(nameof(WorkbenchStrings.SampleDeployFeed1), TimeSpan.FromMinutes(42)),
                FromYou(WorkbenchProgressEntryKind.Step, Text(nameof(WorkbenchStrings.SampleDeployStepServer)), TimeSpan.FromMinutes(25), 1, "server"),
                FromYou(WorkbenchProgressEntryKind.Message, Text(nameof(WorkbenchStrings.SampleDeployFeed2)), TimeSpan.FromMinutes(24), 2),
                Update(nameof(WorkbenchStrings.SampleDeployFeed3), TimeSpan.FromMinutes(18), WorkbenchProgressNoteKind.Success),
                Update(nameof(WorkbenchStrings.SampleDeployFeed4), TimeSpan.FromMinutes(2), WorkbenchProgressNoteKind.Warning)
            ]);

        var feature = new WorkbenchProgressTask(FeatureId,
            Text(nameof(WorkbenchStrings.SampleFeatureTitle)),
            Text(nameof(WorkbenchStrings.SampleFeatureSummary)),
            "Codex",
            WorkbenchProgressStatus.Working,
            now - TimeSpan.FromHours(3),
            now - TimeSpan.FromMinutes(6),
            now - TimeSpan.FromMinutes(1),
            [
                Step("requirements", nameof(WorkbenchStrings.SampleFeatureStepRequirements), WorkbenchProgressStepStatus.Done),
                Step("plan", nameof(WorkbenchStrings.SampleFeatureStepPlan), WorkbenchProgressStepStatus.Done),
                Step("design", nameof(WorkbenchStrings.SampleFeatureStepDesign), WorkbenchProgressStepStatus.Done, WorkbenchProgressOwner.You, link: designPage is null ? null : WorkbenchUrls.Workbench(designPage.Key)),
                Step("ledger", nameof(WorkbenchStrings.SampleFeatureStepLedger), WorkbenchProgressStepStatus.Doing),
                Step("checkout", nameof(WorkbenchStrings.SampleFeatureStepCheckout), WorkbenchProgressStepStatus.Todo),
                Step("tests", nameof(WorkbenchStrings.SampleFeatureStepTests), WorkbenchProgressStepStatus.Todo),
                Step("try", nameof(WorkbenchStrings.SampleStepTryOnTest), WorkbenchProgressStepStatus.Todo, WorkbenchProgressOwner.You)
            ],
            [
                new("expiry", WorkbenchProgressNoteKind.Decision, Text(nameof(WorkbenchStrings.SampleFeatureNoteExpiry))),
                new("spec", WorkbenchProgressNoteKind.Info, Text(nameof(WorkbenchStrings.SampleFeatureNoteSpec)))
            ],
            designPage is null ? [] : [new(Text(nameof(WorkbenchStrings.SampleFeatureLinkDesign)), WorkbenchUrls.Workbench(designPage.Key))],
            [
                new("worth", Text(nameof(WorkbenchStrings.SampleFeatureAskWorth)), Text(nameof(WorkbenchStrings.SampleFeatureAskWorthDetail)), WorkbenchProgressAskKind.Text, [], Text(nameof(WorkbenchStrings.SampleFeatureAskWorthPlaceholder)), now - TimeSpan.FromMinutes(6), null, false),
                new("orders", Text(nameof(WorkbenchStrings.SampleFeatureAskOrders)), null, WorkbenchProgressAskKind.Choices, orderOptions, null, now - TimeSpan.FromMinutes(6), null, false)
            ],
            [
                Update(nameof(WorkbenchStrings.SampleFeatureFeed1), TimeSpan.FromHours(3)),
                FromYou(WorkbenchProgressEntryKind.Step, Text(nameof(WorkbenchStrings.SampleFeatureStepDesign)), TimeSpan.FromMinutes(55), 1, "design"),
                FromYou(WorkbenchProgressEntryKind.Message, Text(nameof(WorkbenchStrings.SampleFeatureFeed2)), TimeSpan.FromMinutes(54), 2),
                Update(nameof(WorkbenchStrings.SampleFeatureFeed3), TimeSpan.FromMinutes(50)),
                Update(nameof(WorkbenchStrings.SampleFeatureFeed4), TimeSpan.FromMinutes(6), WorkbenchProgressNoteKind.Success)
            ]);

        var fix = new WorkbenchProgressTask(FixId,
            Text(nameof(WorkbenchStrings.SampleFixTitle)),
            Text(nameof(WorkbenchStrings.SampleFixSummary)),
            "GitHub Copilot",
            WorkbenchProgressStatus.Done,
            now - TimeSpan.FromDays(1),
            now - TimeSpan.FromMinutes(230),
            now - TimeSpan.FromMinutes(230),
            [
                Step("logs", nameof(WorkbenchStrings.SampleFixStepLogs), WorkbenchProgressStepStatus.Done),
                Step("test", nameof(WorkbenchStrings.SampleFixStepTest), WorkbenchProgressStepStatus.Done),
                Step("fix", nameof(WorkbenchStrings.SampleFixStepFix), WorkbenchProgressStepStatus.Done),
                Step("try", nameof(WorkbenchStrings.SampleStepTryOnTest), WorkbenchProgressStepStatus.Done, WorkbenchProgressOwner.You),
                Step("merge", nameof(WorkbenchStrings.SampleFixStepMerge), WorkbenchProgressStepStatus.Done)
            ],
            [new("done", WorkbenchProgressNoteKind.Success, Text(nameof(WorkbenchStrings.SampleFixNoteDone)))],
            [
                new(Text(nameof(WorkbenchStrings.SampleFixLinkPullRequest)), "https://github.com"),
                new(Text(nameof(WorkbenchStrings.SampleFixLinkTest)), "https://test.example.com")
            ],
            [new("merge", Text(nameof(WorkbenchStrings.SampleFixAskMerge)), null, WorkbenchProgressAskKind.Confirm, [], null, now - TimeSpan.FromHours(5), new([WorkbenchProgressDocument.Yes], null, you, now - TimeSpan.FromMinutes(235)), false)],
            [
                Update(nameof(WorkbenchStrings.SampleFixFeed1), TimeSpan.FromDays(1), WorkbenchProgressNoteKind.Decision),
                Update(nameof(WorkbenchStrings.SampleFixFeed2), TimeSpan.FromHours(5)),
                FromYou(WorkbenchProgressEntryKind.Step, Text(nameof(WorkbenchStrings.SampleStepTryOnTest)), TimeSpan.FromMinutes(240), 1, "try"),
                FromYou(WorkbenchProgressEntryKind.Answer, string.Empty, TimeSpan.FromMinutes(235), 2, "merge"),
                Update(nameof(WorkbenchStrings.SampleFixFeed3), TimeSpan.FromMinutes(230), WorkbenchProgressNoteKind.Success)
            ])
        {
            Report = Text(nameof(WorkbenchStrings.SampleFixReport))
        };

        return [deploy, feature, fix];
    }
}
