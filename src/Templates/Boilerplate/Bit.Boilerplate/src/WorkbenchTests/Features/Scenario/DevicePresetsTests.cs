namespace Boilerplate.WorkbenchTests.Features.Scenario;

[TestClass, TestCategory("UnitTest")]
public class DevicePresetsTests
{
    [TestMethod]
    public void EveryPreset_Should_HaveAKeyOfItsOwnThatALinkCanCarry()
    {
        var keys = DevicePresets.All.Select(d => d.Key).ToArray();

        Assert.AreAllDistinct(keys);

        foreach (var key in keys)
        {
            Assert.MatchesRegex(new Regex("^[a-z0-9-]+$"), key);
            Assert.AreEqual(key, WorkbenchScenario.Parse($"?device={key}").Device, $"{key} doesn't survive a link.");
        }

        Assert.DoesNotContain(DevicePresets.CustomKey, keys);
    }

    [TestMethod]
    public void Find_Should_IgnoreCaseAndFallBackToTheDefault()
    {
        Assert.AreEqual("ipad-mini", DevicePresets.Find("IPAD-Mini").Key);
        Assert.AreEqual(DevicePresets.Default, DevicePresets.Find("unknown"));
        Assert.AreEqual(DevicePresets.Default, DevicePresets.Find(null));
        Assert.AreEqual(DevicePresets.DefaultKey, DevicePresets.Default.Key);
    }

    [TestMethod]
    [DataRow("iphone-18-pro", false, 402, 874, 1206, 2622)]
    [DataRow("iphone-18-pro", true, 874, 402, 2622, 1206)]
    [DataRow("store-apple-iphone-69", false, 440, 956, 1320, 2868)]
    [DataRow("store-play-phone", false, 360, 640, 1080, 1920)]
    [DataRow("store-microsoft", false, 1920, 1080, 3840, 2160)]
    [DataRow("desktop-1536", false, 1536, 864, 1920, 1080)]
    public void AScreenshot_Should_HaveTheDevicePixelSizeTheStoresAskFor(string key, bool rotated, int width, int height, int pixelWidth, int pixelHeight)
    {
        var device = DevicePresets.Find(key);

        Assert.AreEqual((width, height), device.GetViewport(rotated));
        Assert.AreEqual((pixelWidth, pixelHeight), device.GetPixels(rotated));
    }

    [TestMethod]
    public void TheSafeArea_Should_ShrinkForAPhoneOnItsSideAndVanishOnADesktop()
    {
        var phone = DevicePresets.Find("iphone-18-pro");
        var tablet = DevicePresets.Find("ipad-pro-11");
        var desktop = DevicePresets.Find("desktop-1920");

        Assert.AreEqual((62, 34), phone.GetSafeArea(rotated: false));
        Assert.AreEqual((0, 21), phone.GetSafeArea(rotated: true));
        Assert.AreEqual((24, 20), tablet.GetSafeArea(rotated: true));
        Assert.AreEqual((0, 0), desktop.GetSafeArea(rotated: false));
    }

    [TestMethod]
    public void TheKinds_Should_TellHandheldsFromComputers()
    {
        Assert.IsTrue(DevicePresets.Find("galaxy-z-fold7").IsHandheld);
        Assert.IsTrue(DevicePresets.Find("ipad-mini").IsHandheld);
        Assert.IsFalse(DevicePresets.Find("surface-pro").IsHandheld);
        Assert.IsTrue(DevicePresets.Find("iphone-duo-open").IsLandscapeByDefault);
        Assert.IsTrue(DevicePresets.Find("store-play-tablet").IsStorePreset);
        Assert.IsFalse(DevicePresets.Find("pixel-10").IsStorePreset);
    }

    [TestMethod]
    public void EveryStore_Should_OfferItsScreenshotSizes()
    {
        var stores = DevicePresets.All.Where(d => d.IsStorePreset).GroupBy(d => d.Store!).ToDictionary(g => g.Key, g => g.Count());

        Assert.AreSequenceEqual(new[] { "App Store", "Google Play", "Microsoft Store" }, stores.Keys.Order().ToArray());
    }
}
