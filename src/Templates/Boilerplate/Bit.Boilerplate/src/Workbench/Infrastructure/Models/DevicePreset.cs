namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record DevicePreset(string Key,
                                  string Name,
                                  DeviceKind Kind,
                                  int Width,
                                  int Height,
                                  double PixelRatio,
                                  int SafeAreaTop = 0,
                                  int SafeAreaBottom = 0,
                                  int CornerRadius = 0,
                                  DeviceCutout Cutout = DeviceCutout.None,
                                  bool IsAndroid = false,
                                  string? Store = null)
{
    public bool IsStorePreset => Store is not null;

    public bool IsHandheld => Kind is DeviceKind.Phone or DeviceKind.Foldable or DeviceKind.Tablet;

    public bool IsLandscapeByDefault => Width > Height;

    public (int Width, int Height) GetViewport(bool landscape)
    {
        var rotate = landscape != IsLandscapeByDefault;
        return rotate ? (Height, Width) : (Width, Height);
    }

    public (int Width, int Height) GetPixels(bool landscape)
    {
        var (width, height) = GetViewport(landscape);
        return ((int)Math.Round(width * PixelRatio), (int)Math.Round(height * PixelRatio));
    }

    public (int Top, int Bottom) GetSafeArea(bool landscape)
    {
        if (IsHandheld is false)
            return (0, 0);

        return GetViewport(landscape).Width > GetViewport(landscape).Height && Kind is DeviceKind.Phone
            ? (0, Math.Min(SafeAreaBottom, 21))
            : (SafeAreaTop, SafeAreaBottom);
    }
}
