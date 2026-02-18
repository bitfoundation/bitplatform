namespace Bit.BlazorUI;

public partial class BitNav<TItem>
{
    internal void SetIsExpanded(TItem item, bool value)
    {
        if (item is BitNavItem navItem)
        {
            navItem.IsExpanded = value;
        }

        if (item is BitNavOption navOption)
        {
            navOption.IsExpanded = value;
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.IsExpanded.Name, value);
    }

    internal BitNavAriaCurrent GetAriaCurrent(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.AriaCurrent;
        }

        if (item is BitNavOption navOption)
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
        if (item is BitNavItem navItem)
        {
            return navItem.AriaLabel;
        }

        if (item is BitNavOption navOption)
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

    internal string? GetClass(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Class;
        }

        if (item is BitNavOption navOption)
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

    internal List<TItem> GetChildItems(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.ChildItems as List<TItem> ?? [];
        }

        if (item is BitNavOption navOption)
        {
            return navOption.ChildItems as List<TItem> ?? [];
        }

        if (NameSelectors is null) return [];

        if (NameSelectors.ChildItems.Selector is not null)
        {
            return NameSelectors.ChildItems.Selector!(item) ?? [];
        }

        return item.GetValueFromProperty<List<TItem>>(NameSelectors.ChildItems.Name, [])!;
    }

    internal string? GetCollapseAriaLabel(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.CollapseAriaLabel;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.CollapseAriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.CollapseAriaLabel.Selector is not null)
        {
            return NameSelectors.CollapseAriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.CollapseAriaLabel.Name);
    }

    internal object? GetData(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Data;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Data;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Data.Selector is not null)
        {
            return NameSelectors.Data.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Data.Name);
    }

    internal string? GetDescription(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Description;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Description;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Description.Selector is not null)
        {
            return NameSelectors.Description.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Description.Name);
    }

    internal string? GetExpandAriaLabel(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.ExpandAriaLabel;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.ExpandAriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ExpandAriaLabel.Selector is not null)
        {
            return NameSelectors.ExpandAriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.ExpandAriaLabel.Name);
    }

    internal bool GetForceAnchor(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.ForceAnchor;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.ForceAnchor;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.ForceAnchor.Selector is not null)
        {
            return NameSelectors.ForceAnchor.Selector!(item) ?? false;
        }

        return item.GetValueFromProperty(NameSelectors.ForceAnchor.Name, false);
    }

    internal BitIconInfo? GetIcon(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return BitIconInfo.From(navItem.Icon, navItem.IconName);
        }

        if (item is BitNavOption navOption)
        {
            return BitIconInfo.From(navOption.Icon, navOption.IconName);
        }

        if (NameSelectors is null) return null;

        BitIconInfo? icon = null;
        if (NameSelectors.Icon.Selector is not null)
        {
            icon = NameSelectors.Icon.Selector!(item);
        }
        else
        {
            icon = item.GetValueFromProperty<BitIconInfo?>(NameSelectors.Icon.Name);
        }

        string? iconName = null;
        if (NameSelectors.IconName.Selector is not null)
        {
            iconName = NameSelectors.IconName.Selector!(item);
        }
        else
        {
            iconName = item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
        }

        return BitIconInfo.From(icon, iconName);
    }

    internal string? GetIconName(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.IconName;
        }

        if (item is BitNavOption navOption)
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
        if (item is BitNavItem navItem)
        {
            return navItem.IsEnabled;
        }

        if (item is BitNavOption navOption)
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

    internal bool? GetIsExpanded(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.IsExpanded;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.IsExpanded;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsExpanded.Selector is not null)
        {
            return NameSelectors.IsExpanded.Selector!(item) ?? false;
        }

        return item.GetValueFromProperty(NameSelectors.IsExpanded.Name, false);
    }

    internal bool GetIsSeparator(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.IsSeparator;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.IsSeparator;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsSeparator.Selector is not null)
        {
            return NameSelectors.IsSeparator.Selector!(item) ?? false;
        }

        return item.GetValueFromProperty(NameSelectors.IsSeparator.Name, false);
    }

    internal string? GetKey(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Key;
        }

        if (item is BitNavOption navOption)
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

    internal string? GetStyle(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Style;
        }

        if (item is BitNavOption navOption)
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
        if (item is BitNavItem navItem)
        {
            return navItem.Target;
        }

        if (item is BitNavOption navOption)
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
        if (item is BitNavItem navItem)
        {
            return navItem.Template as RenderFragment<TItem>;
        }

        if (item is BitNavOption navOption)
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

    internal BitNavItemTemplateRenderMode GetTemplateRenderMode(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.TemplateRenderMode;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.TemplateRenderMode;
        }

        if (NameSelectors is null) return BitNavItemTemplateRenderMode.Normal;

        if (NameSelectors.TemplateRenderMode.Selector is not null)
        {
            return NameSelectors.TemplateRenderMode.Selector!(item) ?? BitNavItemTemplateRenderMode.Normal;
        }

        return item.GetValueFromProperty(NameSelectors.TemplateRenderMode.Name, BitNavItemTemplateRenderMode.Normal);
    }

    internal string? GetText(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Text;
        }

        if (item is BitNavOption navOption)
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
        if (item is BitNavItem navItem)
        {
            return navItem.Title;
        }

        if (item is BitNavOption navOption)
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
        if (item is BitNavItem navItem)
        {
            return navItem.Url;
        }

        if (item is BitNavOption navOption)
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

    internal IEnumerable<string>? GetAdditionalUrls(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.AdditionalUrls;
        }

        if (item is BitNavOption navOption)
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

    internal BitNavMatch? GetMatch(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Match;
        }

        if (item is BitNavOption navOption)
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
}
