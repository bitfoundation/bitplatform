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

    public (int Width, int Height) GetViewport(bool rotated) => rotated ? (Height, Width) : (Width, Height);

    public bool IsLandscape(bool rotated) => GetViewport(rotated) is var (width, height) && width > height;

    public (int Width, int Height) GetPixels(bool rotated)
    {
        var (width, height) = GetViewport(rotated);
        return ((int)Math.Round(width * PixelRatio), (int)Math.Round(height * PixelRatio));
    }

    public (int Top, int Bottom) GetSafeArea(bool rotated)
    {
        if (IsHandheld is false)
            return (0, 0);

        return IsLandscape(rotated) && Kind is DeviceKind.Phone
            ? (0, Math.Min(SafeAreaBottom, 21))
            : (SafeAreaTop, SafeAreaBottom);
    }
}
