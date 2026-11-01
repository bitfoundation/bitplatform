namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>The kind of device a preset stands for, which decides its frame and whether it counts as handheld.</summary>
public enum DeviceKind
{
    Phone,
    Foldable,
    Tablet,
    Laptop,
    Desktop
}
