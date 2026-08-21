namespace Bit.BlazorUI;

// Reads a single property of an item, whichever of the three input APIs it comes from: the BitNavBarItem
// class, the BitNavBarOption component, or a custom type read through the NameSelectors.
public partial class BitNavBar<TItem>
{
    internal BitNavAriaCurrent GetAriaCurrent(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.AriaCurrent;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.AriaCurrent;
        }

        if (NameSelectors is null) return BitNavAriaCurrent.Page;

        if (NameSelectors.AriaCurrent.Selector is not null)
        {
            return NameSelectors.AriaCurrent.Selector!(item) ?? BitNavAriaCurrent.Page;
        }

        return item.GetValueFromProperty(NameSelectors.AriaCurrent.Name, BitNavAriaCurrent.Page);
    }

    internal string? GetAriaLabel(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.AriaLabel;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.AriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.AriaLabel.Selector is not null)
        {
            return NameSelectors.AriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.AriaLabel.Name);
    }

    internal string? GetBadge(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Badge;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Badge;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Badge.Selector is not null)
        {
            return NameSelectors.Badge.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Badge.Name);
    }

    internal string? GetBadgeAriaLabel(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.BadgeAriaLabel;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.BadgeAriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.BadgeAriaLabel.Selector is not null)
        {
            return NameSelectors.BadgeAriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.BadgeAriaLabel.Name);
    }

    private string? GetClass(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Class;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    internal bool GetDot(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Dot;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Dot;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.Dot.Selector is not null)
        {
            return NameSelectors.Dot.Selector!(item) ?? false;
        }

        return item.GetValueFromProperty(NameSelectors.Dot.Name, false);
    }

    internal BitIconInfo? GetIcon(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Icon;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Icon;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Icon.Selector is not null)
        {
            return NameSelectors.Icon.Selector!(item);
        }

        return item.GetValueFromProperty<BitIconInfo?>(NameSelectors.Icon.Name);
    }

    internal string? GetIconName(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.IconName;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.IconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.IconName.Selector is not null)
        {
            return NameSelectors.IconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
    }

    internal bool GetIsEnabled(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.IsEnabled;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item) ?? true;
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    private string? GetKey(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Key;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Key;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Key.Selector is not null)
        {
            return NameSelectors.Key.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Key.Name);
    }

    private BitNavMatch? GetMatch(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Match;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Match;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Match.Selector is not null)
        {
            return NameSelectors.Match.Selector!(item);
        }

        return item.GetValueFromProperty<BitNavMatch?>(NameSelectors.Match.Name);
    }

    internal BitIconInfo? GetSelectedIcon(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.SelectedIcon;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.SelectedIcon;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.SelectedIcon.Selector is not null)
        {
            return NameSelectors.SelectedIcon.Selector!(item);
        }

        return item.GetValueFromProperty<BitIconInfo?>(NameSelectors.SelectedIcon.Name);
    }

    internal string? GetSelectedIconName(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.SelectedIconName;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.SelectedIconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.SelectedIconName.Selector is not null)
        {
            return NameSelectors.SelectedIconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.SelectedIconName.Name);
    }

    private string? GetStyle(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Style;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    internal string? GetTarget(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Target;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Target;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Target.Selector is not null)
        {
            return NameSelectors.Target.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Target.Name);
    }

    internal RenderFragment<TItem>? GetTemplate(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Template as RenderFragment<TItem>;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Template as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Template.Selector is not null)
        {
            return NameSelectors.Template.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Template.Name);
    }

    internal string? GetText(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Text;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Text;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Text.Selector is not null)
        {
            return NameSelectors.Text.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Text.Name);
    }

    internal string? GetTitle(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Title;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Title;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Title.Selector is not null)
        {
            return NameSelectors.Title.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Title.Name);
    }

    internal string? GetUrl(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Url;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.Url;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Url.Selector is not null)
        {
            return NameSelectors.Url.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Url.Name);
    }

    private IEnumerable<string>? GetAdditionalUrls(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.AdditionalUrls;
        }

        if (item is BitNavBarOption navOption)
        {
            return navOption.AdditionalUrls;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.AdditionalUrls.Selector is not null)
        {
            return NameSelectors.AdditionalUrls.Selector!(item);
        }

        return item.GetValueFromProperty<IEnumerable<string>?>(NameSelectors.AdditionalUrls.Name);
    }
}
