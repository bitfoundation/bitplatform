namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// What the app's own pages keep on the device a view stands for (See <see cref="WorkbenchViewStorage"/>), saved per view,
/// so a reload and a screenshot keep it, and two views side by side keep their own.
/// </summary>
[BlexStore(Name = "device", Persist = true)]
public partial class WorkbenchDeviceStore
{
    [BlexState] private Dictionary<string, string> _items = [];

    [BlexAction]
    private void OnSetItem(string key, string? value)
    {
        var items = new Dictionary<string, string>(Items);

        if (value is null)
        {
            items.Remove(key);
        }
        else
        {
            items[key] = value;
        }

        Items = items;
    }

    [BlexAction] private void OnClear() => Items = [];
}
