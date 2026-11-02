namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>
/// The shell's store holds the views and their settings, which the address mirrors and links carry, so its actions are
/// what decide what a reviewer sees after every click.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchStoreTests
{
    private static WorkbenchStore NewStore()
    {
        var store = new WorkbenchStore();
        new BlexManager().Register(store);
        return store;
    }

    [TestMethod]
    public void ANewStore_Should_ShowOneDefaultViewInBrowseMode()
    {
        var store = NewStore();

        Assert.HasCount(1, store.Panes);
        Assert.AreEqual("1", store.SelectedPane.Id);
        Assert.AreEqual(new WorkbenchScenario(), store.SelectedPane.Scenario);
        Assert.AreEqual("fit", store.Zoom);
        Assert.AreEqual(WorkbenchModes.Browse, store.Mode);
        Assert.AreEqual(WorkbenchModes.ViewColumn, store.Column);
        Assert.IsFalse(store.IsNavigatorCollapsed);
        Assert.IsFalse(store.IsInspectorCollapsed);
        Assert.AreEqual(string.Empty, store.QueryString);
    }

    [TestMethod]
    public void TheToggles_Should_FoldAndUnfoldThePageListAndTheSideColumn()
    {
        var store = NewStore();

        store.ToggleNavigator();
        store.ToggleInspector();

        Assert.IsTrue(store.IsNavigatorCollapsed);
        Assert.IsTrue(store.IsInspectorCollapsed);

        store.ToggleNavigator();

        Assert.IsFalse(store.IsNavigatorCollapsed);
        Assert.IsTrue(store.IsInspectorCollapsed);
    }

    [TestMethod]
    public void EditTextAndATab_Should_BringTheFoldedSideColumnBack()
    {
        var store = NewStore();

        store.ToggleInspector();
        store.SetMode(WorkbenchModes.Comment);

        Assert.IsTrue(store.IsInspectorCollapsed, "Comment mode works on the canvas, so the column stays folded.");

        store.SetMode(WorkbenchModes.Text);

        Assert.IsFalse(store.IsInspectorCollapsed);
        Assert.AreEqual(WorkbenchModes.Text, store.Mode);

        store.ToggleInspector();
        store.ShowColumn(WorkbenchModes.ReviewColumn);

        Assert.IsFalse(store.IsInspectorCollapsed);
        Assert.AreEqual(WorkbenchModes.ReviewColumn, store.Column);
    }

    [TestMethod]
    public void AddPane_Should_AddTheSelectedViewOnTheNextKindOfDevice()
    {
        var store = NewStore();

        store.ChangeScenario(new() { Theme = WorkbenchThemeMode.Dark, Rotated = true });
        store.AddPane();

        Assert.HasCount(2, store.Panes);
        Assert.AreEqual("2", store.SelectedPaneId, "The new view is selected.");
        Assert.AreEqual("ipad-pro-11", store.SelectedPane.Scenario.Device, "A phone gets a tablet.");
        Assert.AreEqual(WorkbenchThemeMode.Dark, store.SelectedPane.Scenario.Theme, "The new view copies the selected one.");
        Assert.IsFalse(store.SelectedPane.Scenario.Rotated);

        store.AddPane();
        Assert.AreEqual("desktop-1536", store.SelectedPane.Scenario.Device, "A tablet gets a desktop.");

        store.AddPane();
        Assert.AreEqual(DevicePresets.DefaultKey, store.SelectedPane.Scenario.Device, "Anything else gets a phone.");

        Assert.AreSequenceEqual(new[] { "1", "2", "3", "4" }, store.Panes.Select(p => p.Id).ToArray());
    }

    [TestMethod]
    public void RemovePane_Should_KeepTheLastViewAndMoveTheSelectionToTheFirst()
    {
        var store = NewStore();

        store.RemovePane("1");
        Assert.HasCount(1, store.Panes, "The last view can't be removed.");

        store.AddPane();
        store.RemovePane("2");

        Assert.HasCount(1, store.Panes);
        Assert.AreEqual("1", store.SelectedPaneId);
    }

    [TestMethod]
    public void SelectPane_Should_IgnoreAViewThatIsGone()
    {
        var store = NewStore();

        store.AddPane();
        store.SelectPane("1");
        store.SelectPane("42");

        Assert.AreEqual("1", store.SelectedPaneId);
    }

    [TestMethod]
    public void ChangeScenario_Should_CopyWhatChangedToTheOtherViewsButNeverTheirDevice()
    {
        var store = NewStore();

        store.AddPane();
        store.AddPane();
        store.SelectPane("1");

        store.ChangeScenario(store.SelectedPane.Scenario with { Theme = WorkbenchThemeMode.Dark, Culture = "fa-IR", Device = "pixel-10", Rotated = true });

        var others = store.Panes.Where(p => p.Id != "1").Select(p => p.Scenario).ToArray();

        Assert.AreEqual("pixel-10", store.SelectedPane.Scenario.Device);
        Assert.IsTrue(others.All(s => s.Theme is WorkbenchThemeMode.Dark && s.Culture == "fa-IR"));
        Assert.AreSequenceEqual(new[] { "ipad-pro-11", "desktop-1536" }, others.Select(s => s.Device).ToArray());
        Assert.IsTrue(others.All(s => s.Rotated is false));
    }

    [TestMethod]
    public void ChangeScenario_Should_CopyOnlyTheSwitchThatChanged()
    {
        var store = NewStore();

        store.CompareSwitch("amount", ["normal", "few", "many"]);
        store.SelectPane(store.Panes[0].Id);

        store.ChangeScenario(store.SelectedPane.Scenario.WithSwitch("names", "long"));

        Assert.AreSequenceEqual(new string?[] { null, "few", "many" }, store.Panes.Select(p => p.Scenario.GetSwitch("amount")).ToArray(),
            "Each view keeps its own value of the switch it was compared on.");
        Assert.IsTrue(store.Panes.All(p => p.Scenario.GetSwitch("names") is "long"));
    }

    [TestMethod]
    public void ReportScenario_Should_ChangeOnlyTheViewThatReportedIt()
    {
        var store = NewStore();

        store.AddPane();
        store.ReportScenario("1", new() { Theme = WorkbenchThemeMode.Dark });

        Assert.AreEqual(WorkbenchThemeMode.Dark, store.Panes[0].Scenario.Theme);
        Assert.AreEqual(WorkbenchThemeMode.Light, store.Panes[1].Scenario.Theme);
    }

    [TestMethod]
    public void ResizeAndRotate_Should_ChangeOnlyThatView()
    {
        var store = NewStore();

        store.AddPane();
        store.ResizePane("1", 900, 700);
        store.RotatePane("2");

        Assert.AreEqual(DevicePresets.CustomKey, store.Panes[0].Scenario.Device);
        Assert.AreEqual((900, 700), (store.Panes[0].Scenario.Width, store.Panes[0].Scenario.Height));
        Assert.IsTrue(store.Panes[1].Scenario.Rotated);
        Assert.IsFalse(store.Panes[0].Scenario.Rotated);
    }

    [TestMethod]
    public void CompareDevices_Should_ShowAPhoneATabletAndADesktopKeepingTheSelectedViewsFrame()
    {
        var store = NewStore();

        store.AddPane();
        store.RotatePane("2");
        store.Compare("devices");

        Assert.AreSequenceEqual(new[] { DevicePresets.DefaultKey, "ipad-pro-11", "desktop-1536" }, store.Panes.Select(p => p.Scenario.Device).ToArray());
        Assert.AreEqual("2", store.Panes[0].Id, "The first view keeps the selected one's id, so its frame is reused.");
        Assert.AreEqual("2", store.SelectedPaneId);
        Assert.IsTrue(store.Panes.All(p => p.Scenario.Rotated is false));
    }

    [TestMethod]
    [DataRow("themes", 2)]
    [DataRow("states", 4)]
    [DataRow("accounts", 3)]
    [DataRow("single", 1)]
    [DataRow("whatever", 1)]
    public void Compare_Should_SpreadTheSelectedViewOverOneSetting(string mode, int views)
    {
        var store = NewStore();

        store.Compare(mode);

        Assert.HasCount(views, store.Panes);
        Assert.HasCount(views, store.Panes.Select(p => p.Scenario.ToQueryString()).Distinct());
    }

    [TestMethod]
    public void CompareStates_Should_ShowLiveLoadingEmptyAndError()
    {
        var store = NewStore();

        store.Compare("states");

        Assert.AreSequenceEqual(new[] { WorkbenchDataState.Live, WorkbenchDataState.Loading, WorkbenchDataState.Empty, WorkbenchDataState.Error },
            store.Panes.Select(p => p.Scenario.Data).ToArray());
    }

    [TestMethod]
    [TestCategory("Localization")]
    public void CompareLanguages_Should_ShowTheDefaultARightToLeftAndAnotherLanguage()
    {
        var store = NewStore();

        store.Compare("languages");

        var cultures = store.Panes.Select(p => p.Scenario.Culture).ToArray();

        Assert.HasCount(3, cultures);
        Assert.IsNull(cultures[0]);
        Assert.IsTrue(store.Panes[1].Scenario.IsRtl);
        Assert.IsFalse(store.Panes[2].Scenario.IsRtl);
        Assert.IsFalse(cultures[2]!.StartsWith("en", StringComparison.Ordinal), "The third language isn't another English.");
    }

    [TestMethod]
    public void CompareVariantsAndSwitches_Should_ShowTheDefaultFirst()
    {
        var store = NewStore();

        store.CompareVariants(["table", "cards"]);

        Assert.AreSequenceEqual(new[] { null, "cards" }, store.Panes.Select(p => p.Scenario.Variant).ToArray());

        store.Compare("single");
        store.CompareSwitch("names", ["short", "long"]);

        Assert.AreSequenceEqual(new[] { null, "names=long" }, store.Panes.Select(p => p.Scenario.Switches).ToArray());
    }

    [TestMethod]
    public void TheQueryString_Should_PackSeveralViewsAndAZoom()
    {
        var store = NewStore();

        store.ChangeScenario(new() { Theme = WorkbenchThemeMode.Dark });
        Assert.AreEqual("?theme=dark", store.QueryString);

        store.SetZoom("50");
        Assert.AreEqual("?theme=dark&zoom=50", store.QueryString);

        store.AddPane();
        Assert.StartsWith("?panes=", store.QueryString);

        var restored = NewStore();
        restored.OpenUrl(store.QueryString);

        Assert.AreSequenceEqual(store.Panes.Select(p => p.Scenario).ToArray(), restored.Panes.Select(p => p.Scenario).ToArray());
        Assert.AreEqual("50", restored.Zoom);
    }

    [TestMethod]
    public void OtherPageQueryString_Should_DropTheDesignOptionAndSwitches()
    {
        var store = NewStore();

        store.ChangeScenario(new WorkbenchScenario { Theme = WorkbenchThemeMode.Dark, Variant = "cards" }.WithSwitch("amount", "few"));

        Assert.AreEqual("?theme=dark", store.OtherPageQueryString);
    }

    [TestMethod]
    public void OpenUrl_Should_LeaveTheViewsAloneWhenTheAddressNamesNoScenario()
    {
        var store = NewStore();

        store.AddPane();
        store.OpenUrl("?zoom=75&utm_source=mail");

        Assert.HasCount(2, store.Panes);
        Assert.AreEqual("75", store.Zoom);
    }

    [TestMethod]
    public void OpenUrl_Should_ReplaceTheViewsWithTheOnesALinkNames()
    {
        var store = NewStore();

        store.AddPane();
        store.OpenUrl("?data=empty&device=ipad-mini");

        Assert.HasCount(1, store.Panes);
        Assert.AreEqual(WorkbenchDataState.Empty, store.SelectedPane.Scenario.Data);
        Assert.AreEqual("ipad-mini", store.SelectedPane.Scenario.Device);
        Assert.AreEqual("2", store.SelectedPaneId, "The selected view's frame is reused.");
    }

    [TestMethod]
    public void OpenUrl_Should_TakeALinkThatOnlySetsASwitch()
    {
        var store = NewStore();

        store.OpenUrl("?sw-amount=few");

        Assert.AreEqual("few", store.SelectedPane.Scenario.GetSwitch("amount"));
    }

    [TestMethod]
    public void OpenUrl_Should_KeepTheViewsWhenTheLinkNamesTheSameOnes()
    {
        var store = NewStore();

        store.ChangeScenario(new() { Theme = WorkbenchThemeMode.Dark });
        var lastPaneId = store.LastPaneId;

        store.OpenUrl("?theme=dark");

        Assert.AreEqual(lastPaneId, store.LastPaneId, "No view was created again.");
        Assert.AreEqual("1", store.SelectedPaneId);
    }

    [TestMethod]
    public void TheReviewerSettings_Should_BeKept()
    {
        var store = NewStore();

        store.NameReviewer("  Sarah  ");
        Assert.AreEqual("Sarah", store.ReviewerName);

        store.NameReviewer("   ");
        Assert.IsNull(store.ReviewerName);

        store.ToggleReplySound();
        store.SetReplyNotifications(true);
        store.MarkAskedToNotify();

        Assert.IsTrue(store.IsReplySoundMuted);
        Assert.IsTrue(store.ReplyNotifications);
        Assert.IsTrue(store.WasAskedToNotify);
    }
}
