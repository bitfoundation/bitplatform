namespace Boilerplate.Workbench.Infrastructure.Models;

public static class DevicePresets
{
    public const string DefaultKey = "iphone-17-pro";

    public const string CustomKey = "custom";

    public const int MinCustomSize = 240;

    public const int MaxCustomWidth = 3840;

    public const int MaxCustomHeight = 2400;

    public static IReadOnlyList<DevicePreset> All { get; } =
    [
        new("iphone-17-pro-max", "iPhone 17 Pro Max", DeviceKind.Phone, 440, 956, 3, 62, 34, 55, DeviceCutout.Island),
        new("iphone-17-pro", "iPhone 17 Pro", DeviceKind.Phone, 402, 874, 3, 62, 34, 55, DeviceCutout.Island),
        new("iphone-air", "iPhone Air", DeviceKind.Phone, 420, 912, 3, 68, 34, 55, DeviceCutout.Island),
        new("iphone-16", "iPhone 16", DeviceKind.Phone, 393, 852, 3, 59, 34, 55, DeviceCutout.Island),
        new("iphone-se", "iPhone SE", DeviceKind.Phone, 375, 667, 2, 20, 0, 0, DeviceCutout.HomeButton),
        new("pixel-10-pro", "Pixel 10 Pro", DeviceKind.Phone, 427, 952, 3, 48, 24, 44, DeviceCutout.PunchHole, IsAndroid: true),
        new("pixel-10", "Pixel 10", DeviceKind.Phone, 412, 924, 2.625, 48, 24, 44, DeviceCutout.PunchHole, IsAndroid: true),
        new("galaxy-s25-ultra", "Galaxy S25 Ultra", DeviceKind.Phone, 412, 891, 3.5, 40, 24, 20, DeviceCutout.PunchHole, IsAndroid: true),
        new("galaxy-z-fold7", "Galaxy Z Fold7", DeviceKind.Foldable, 984, 1092, 2, 32, 24, 24, DeviceCutout.PunchHole, IsAndroid: true),
        new("ipad-pro-13", "iPad Pro 13\"", DeviceKind.Tablet, 1032, 1376, 2, 24, 20, 18),
        new("ipad-pro-11", "iPad Pro 11\"", DeviceKind.Tablet, 834, 1210, 2, 24, 20, 18),
        new("ipad-air-11", "iPad Air 11\"", DeviceKind.Tablet, 820, 1180, 2, 24, 20, 18),
        new("ipad-mini", "iPad mini", DeviceKind.Tablet, 744, 1133, 2, 24, 20, 18),
        new("galaxy-tab-s9", "Galaxy Tab S9", DeviceKind.Tablet, 640, 1024, 2.5, 24, 16, 16, IsAndroid: true),
        new("surface-pro", "Surface Pro", DeviceKind.Laptop, 1440, 960, 2),
        new("laptop-hidpi", "Laptop (HiDPI)", DeviceKind.Laptop, 1440, 900, 2),
        new("laptop-1366", "Laptop 1366×768", DeviceKind.Laptop, 1366, 768, 1),
        new("desktop-1536", "Desktop 1536×864", DeviceKind.Desktop, 1536, 864, 1.25),
        new("desktop-1920", "Desktop 1920×1080", DeviceKind.Desktop, 1920, 1080, 1),
        new("desktop-2560", "Desktop 2560×1440", DeviceKind.Desktop, 2560, 1440, 1),

        new("store-apple-iphone-69", "iPhone 6.9\"", DeviceKind.Phone, 440, 956, 3, 62, 34, 55, DeviceCutout.Island, Store: "App Store"),
        new("store-apple-iphone-63", "iPhone 6.3\"", DeviceKind.Phone, 402, 874, 3, 62, 34, 55, DeviceCutout.Island, Store: "App Store"),
        new("store-apple-ipad-13", "iPad 13\"", DeviceKind.Tablet, 1032, 1376, 2, 24, 20, 18, Store: "App Store"),
        new("store-apple-mac", "Mac", DeviceKind.Laptop, 1440, 900, 2, Store: "App Store"),
        new("store-play-phone", "Phone 9:16", DeviceKind.Phone, 360, 640, 3, 24, 16, 0, DeviceCutout.None, IsAndroid: true, Store: "Google Play"),
        new("store-play-tablet", "Tablet 16:9", DeviceKind.Tablet, 1280, 720, 2, 24, 16, 0, DeviceCutout.None, IsAndroid: true, Store: "Google Play"),
        new("store-microsoft", "Desktop 4K", DeviceKind.Desktop, 1920, 1080, 2, Store: "Microsoft Store"),
    ];

    public static DevicePreset Default { get; } = All.First(d => d.Key == DefaultKey);

    public static DevicePreset Find(string? key) => All.FirstOrDefault(d => string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase)) ?? Default;

    public static DevicePreset Custom(int width, int height)
    {
        width = width is 0 ? 1024 : Math.Clamp(width, MinCustomSize, MaxCustomWidth);
        height = height is 0 ? 768 : Math.Clamp(height, MinCustomSize, MaxCustomHeight);

        return new(CustomKey, "Custom", DeviceKind.Desktop, width, height, 1);
    }
}
