namespace Boilerplate.WorkbenchTests.Features.Scenario;

/// <summary>
/// The scenario is what links, previews, screenshots and comments carry in their query strings, so it has to survive the
/// trip unchanged and turn whatever a hand-edited link holds into a valid view.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchScenarioTests
{
    [TestMethod]
    public void TheDefaultView_Should_HaveAnEmptyQueryString()
    {
        Assert.AreEqual(string.Empty, new WorkbenchScenario().ToQueryString());
        Assert.AreEqual(new WorkbenchScenario(), WorkbenchScenario.Parse(null));
        Assert.AreEqual(new WorkbenchScenario(), WorkbenchScenario.Parse(string.Empty));
    }

    [TestMethod]
    [TestCategory("Localization")]
    public void EverySetting_Should_RoundTripThroughTheQueryString()
    {
        var scenario = new WorkbenchScenario
        {
            Data = WorkbenchDataState.Mixed,
            Theme = WorkbenchThemeMode.Dark,
            Culture = "fa-IR",
            Chrome = false,
            Auth = WorkbenchAuthState.MultiAccount,
            User = 2,
            Device = "galaxy-s26",
            Rotated = true,
            Frame = false,
            Backdrop = WorkbenchBackdrop.Ocean,
            Caption = "Every sale, live",
            Variant = "cards",
            Switches = "amount=few;names=long",
            Accent = "#12ab9f",
            TextScale = 130,
            ReducedMotion = true,
            Vision = WorkbenchVision.Deuteranopia
        };

        var query = scenario.ToQueryString();

        Assert.AreEqual(scenario, WorkbenchScenario.Parse(query));
        Assert.AreEqual(query, WorkbenchScenario.Parse(query).ToQueryString());
    }

    [TestMethod]
    [TestCategory("Localization")]
    public void TheQueryString_Should_UseTheKeysTheSkillDocuments()
    {
        var query = new WorkbenchScenario { Data = WorkbenchDataState.Error, Theme = WorkbenchThemeMode.Dark, Culture = "nl-NL", Auth = WorkbenchAuthState.SignedOut, Device = "ipad-mini" }.ToQueryString();

        Assert.AreEqual("?data=error&theme=dark&culture=nl-NL&auth=signedout&device=ipad-mini", query);
    }

    [TestMethod]
    [DataRow("?data=nonsense&theme=purple&auth=maybe&backdrop=space&vision=x-ray")]
    [DataRow("?chrome=perhaps&rotated=1&frame=yes")]
    [DataRow("?user=99&text=150&culture=xx-XX&device=nokia-3310")]
    [DataRow("?user=-1&text=-100&accent=red&variant=Not%20A%20Token")]
    [DataRow("?data=42&theme=7&auth=1&backdrop=3&vision=2")]
    [DataRow("?data=live,error&theme=-1")]
    public void AnInvalidValue_Should_KeepItsDefault(string query)
    {
        var scenario = WorkbenchScenario.Parse(query);

        Assert.AreEqual(new WorkbenchScenario(), scenario);
    }

    [TestMethod]
    [TestCategory("Localization")]
    public void TheEnumValues_Should_BeReadIgnoringCase()
    {
        var scenario = WorkbenchScenario.Parse("?data=EMPTY&theme=Dark&auth=SignedOut&device=IPAD-MINI&culture=FA-ir");

        Assert.AreEqual(WorkbenchDataState.Empty, scenario.Data);
        Assert.AreEqual(WorkbenchThemeMode.Dark, scenario.Theme);
        Assert.AreEqual(WorkbenchAuthState.SignedOut, scenario.Auth);
        Assert.AreEqual("ipad-mini", scenario.Device);
        Assert.AreEqual("fa-IR", scenario.Culture);
    }

    [TestMethod]
    public void ACaption_Should_BeTrimmedCutAndOnlyWrittenWithABackdrop()
    {
        var longCaption = new string('x', WorkbenchScenario.MaxCaptionLength + 50);

        Assert.AreEqual(WorkbenchScenario.MaxCaptionLength, WorkbenchScenario.Parse($"?caption={longCaption}").Caption!.Length);
        Assert.AreEqual("Hello", WorkbenchScenario.Parse("?caption=%20%20Hello%20%20").Caption);

        Assert.AreEqual(string.Empty, new WorkbenchScenario { Caption = "Hello" }.ToQueryString());
        Assert.AreEqual("?backdrop=brand&caption=Hello", new WorkbenchScenario { Backdrop = WorkbenchBackdrop.Brand, Caption = "Hello" }.ToQueryString());
    }

    [TestMethod]
    [DataRow("12AB9F", "#12ab9f")]
    [DataRow("#12ab9f", "#12ab9f")]
    [DataRow("  #ABCDEF ", "#abcdef")]
    [DataRow("#12ab9", null)]
    [DataRow("red", null)]
    [DataRow("#12ab9fff", null)]
    [DataRow(null, null)]
    public void AnAccent_Should_BeReadAsALowercaseHexColorOrNotAtAll(string? value, string? expected)
    {
        Assert.AreEqual(expected, WorkbenchScenario.ReadAccent(value));
    }

    [TestMethod]
    public void AnAccent_Should_TravelWithoutItsHash()
    {
        Assert.AreEqual("?accent=12ab9f", new WorkbenchScenario { Accent = "#12ab9f" }.ToQueryString());
    }

    [TestMethod]
    public void Switches_Should_BeSortedSoEqualSwitchesGiveEqualLinks()
    {
        var first = WorkbenchScenario.Parse("?sw-names=long&sw-amount=few");
        var second = WorkbenchScenario.Parse("?sw-amount=few&sw-names=long");

        Assert.AreEqual("amount=few;names=long", first.Switches);
        Assert.AreEqual(first, second);
        Assert.AreEqual("?sw-amount=few&sw-names=long", first.ToQueryString());
    }

    [TestMethod]
    public void ASwitch_Should_KeepOnlyShortLowercaseTokens()
    {
        var scenario = WorkbenchScenario.Parse($"?sw-amount=FEW&sw-Names=long&sw-ok=yes&sw-x={new string('a', 33)}");

        Assert.AreEqual("ok=yes", scenario.Switches);
    }

    [TestMethod]
    public void WithSwitch_Should_SetReplaceAndRemoveOneSwitch()
    {
        var scenario = new WorkbenchScenario().WithSwitch("amount", "few").WithSwitch("names", "long");

        Assert.AreEqual("few", scenario.GetSwitch("amount"));
        Assert.AreEqual("many", scenario.WithSwitch("amount", "many").GetSwitch("amount"));
        Assert.AreEqual("names=long", scenario.WithSwitch("amount", null).Switches);
        Assert.IsNull(scenario.WithSwitch("amount", null).WithSwitch("names", null).Switches);
        Assert.IsNull(scenario.GetSwitch("missing"));
    }

    [TestMethod]
    public void ACustomSize_Should_BeClampedToTheCustomLimits()
    {
        var tooSmall = new WorkbenchScenario().WithSize(10, 10);
        var tooLarge = new WorkbenchScenario().WithSize(100_000, 100_000);

        Assert.AreEqual(DevicePresets.CustomKey, tooSmall.Device);
        Assert.AreEqual((DevicePresets.MinCustomSize, DevicePresets.MinCustomSize), (tooSmall.Width, tooSmall.Height));
        Assert.AreEqual((DevicePresets.MaxCustomWidth, DevicePresets.MaxCustomHeight), (tooLarge.Width, tooLarge.Height));
    }

    [TestMethod]
    public void ACustomSize_Should_RoundTripThroughItsSizeKey()
    {
        var scenario = new WorkbenchScenario { Rotated = true }.WithSize(1100, 700);

        Assert.IsFalse(scenario.Rotated, "Resizing a view puts it upright.");

        var query = (scenario with { Rotated = true }).ToQueryString();
        var parsed = WorkbenchScenario.Parse(query);

        Assert.AreEqual("?device=custom&size=1100x700&rotated=true", query);
        Assert.AreEqual((1100, 700), (parsed.Width, parsed.Height));
        Assert.IsTrue(parsed.Rotated, "A custom device keeps its rotation from the link.");
        Assert.AreEqual(1100, parsed.DevicePreset.Width);
    }

    [TestMethod]
    [DataRow("?device=custom", 1024, 768)]
    [DataRow("?device=custom&size=garbage", 1024, 768)]
    [DataRow("?device=custom&size=800x", 1024, 768)]
    public void ACustomDeviceWithoutAValidSize_Should_GetTheDefaultCustomSize(string query, int width, int height)
    {
        var scenario = WorkbenchScenario.Parse(query);

        Assert.AreEqual((width, height), (scenario.DevicePreset.Width, scenario.DevicePreset.Height));
    }

    [TestMethod]
    [DataRow("fa-IR", true)]
    [DataRow("ar-SA", true)]
    [DataRow("nl-NL", false)]
    [DataRow(null, false)]
    [TestCategory("Localization")]
    public void IsRtl_Should_FollowTheCulture(string? culture, bool isRtl)
    {
        Assert.AreEqual(isRtl, new WorkbenchScenario { Culture = culture }.IsRtl);
    }
}
