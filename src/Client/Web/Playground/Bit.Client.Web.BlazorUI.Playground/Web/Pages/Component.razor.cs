using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class Component
    {
        private bool IsCheckBoxChecked = false;
        private bool IsCheckBoxIndeterminate = true;
        private bool IsCheckBoxIndeterminateInCode = true;
        private bool IsToggleChecked = true;
        private bool IsToggleUnChecked = false;

        private bool IsMessageBarHidden = false;
        private TextFieldType InputType = TextFieldType.Password;

        private readonly List<NavLink> BasicNavLinks = new()
        {
            new NavLink(name: "Home",
                        url: "http://example.com",
                        key: "key1",
                        title: "Home",
                        targetType: NavLinkTargetType.Blank,
                        isExpanded: true,
                        collapseAriaLabel: "Collapse Home section",
                        links: new List<NavLink>
                        {
                            new NavLink(
                                name: "Activity",
                                url: "http://msn.com",
                                key: "key1-1",
                                title:"Activity",
                                links:new List<NavLink>
                                {
                                    new NavLink(
                                        name: "Activity",
                                        title:"Activity",
                                        url: "http://msn.com",
                                        key: "key1-1-1"),
                                     new NavLink(name: "MSN",title:"MSN", url: "http://msn.com", key: "key1-1-2",disabled:true)
                                }),
                            new NavLink(name: "MSN",title:"MSN", url: "http://msn.com", key: "key1-2",disabled:true),
                        }),
            new NavLink(name: "Documents", title: "Documents", url: "http://example.com", key: "key2", targetType: NavLinkTargetType.Blank, isExpanded: true),
            new NavLink(name: "Pages", title: "Pages", url: "http://msn.com", key: "key3", targetType: NavLinkTargetType.Parent),
            new NavLink(name: "Notebook", title: "Notebook", url: "http://msn.com", key: "key4", disabled: true),
            new NavLink(name: "Communication and Media", title: "Communication and Media", url: "http://msn.com", key: "key5", targetType: NavLinkTargetType.Top),
            new NavLink(name: "News", title: "News", url: "http://msn.com", key: "key6", icon: "News", targetType: NavLinkTargetType.Self),
        };

        private readonly List<NavLink> BasicNoToolTipNavLinks = new()
        {
            new NavLink(name: "Home",
                        url: "http://example.com",
                        key: "key1",
                        targetType: NavLinkTargetType.Blank,
                        isExpanded: true,
                        collapseAriaLabel: "Collapse Home section",
                        links: new List<NavLink>
                        {
                            new NavLink(
                                name: "Activity",
                                url: "http://msn.com",
                                key: "key1-1",
                                links:new List<NavLink>
                                {
                                    new NavLink(
                                        name: "Activity",
                                        url: "http://msn.com",
                                        key: "key1-1-1"),
                                     new NavLink(name: "MSN", url: "http://msn.com", key: "key1-1-2",disabled:true)
                                }),
                            new NavLink(name: "MSN", url: "http://msn.com", key: "key1-2",disabled:true),
                        }),
            new NavLink(name: "Shared Documents and Files", url: "http://example.com", key: "key2", targetType: NavLinkTargetType.Blank, isExpanded: true),
            new NavLink(name: "Pages", url: "http://msn.com", key: "key3", targetType: NavLinkTargetType.Parent),
            new NavLink(name: "Notebook", url: "http://msn.com", key: "key4", disabled: true),
            new NavLink(name: "Communication and Media", url: "http://msn.com", key: "key5", targetType: NavLinkTargetType.Top),
            new NavLink(name: "News", url: "http://msn.com", key: "key6", icon: "News", targetType: NavLinkTargetType.Self),
        };

        private readonly List<NavLink> BasicNoUrlNavLinks = new()
        {
            new NavLink(name: "Basic components", key: "key1", collapseAriaLabel: "Collapse Basic components section", isExpanded: true,
                        links: new List<NavLink>
                        {
                            new NavLink(name: "ActivityItem", key: "ActivityItem",url:"#/examples/activityitem"),
                            new NavLink(name: "Breadcrumb", key: "Breadcrumb",url:"#/examples/breadcrumb"),
                            new NavLink(name: "Button", key: "Button",url:"#/examples/button")
                        }),
            new NavLink(name: "Extended components", key: "key2", collapseAriaLabel: "Collapse Extended components section", isExpanded: true,
                        links: new List<NavLink>
                        {
                            new NavLink(name: "ColorPicker", key: "ColorPicker",url:"#/examples/colorpicker"),
                            new NavLink(name: "ExtendedPeoplePicker", key: "ExtendedPeoplePicker",url:"#/examples/extendedpeoplepicker"),
                            new NavLink(name: "GroupedList", key: "GroupedList",url:"#/examples/groupedlist")
                        }),
            new NavLink(name: "Utilities", key: "key3", collapseAriaLabel: "Collapse Utilities section", isExpanded: true,
                        links: new List<NavLink>
                        {
                            new NavLink(name: "FocusTrapZone", key: "FocusTrapZone",url:"#/examples/focustrapzone"),
                            new NavLink(name: "FocusZone", key: "FocusZone",url:"#/examples/focuszone"),
                            new NavLink(name: "MarqueeSelection", key: "MarqueeSelection",url:"#/examples/marqueeselection")
                        }),
        };

        private readonly List<NavLink> NestedLinks = new()
        {
            new NavLink(name: "Parent link 1",
                       url: "http://example.com",
                       key: "key1",
                       title: "Parent link 1",
                       collapseAriaLabel: "Collapse Parent link 1",
                       links: new List<NavLink>
                       {
                            new NavLink(
                                name: "Child link 1",
                                url: "http://msn.com",
                                key: "key1-1",
                                title:"Child link 1",
                                links:new List<NavLink>
                                {
                                    new NavLink(name: "3rd level link 1",title:"3rd level link 1",url: "http://msn.com",key: "key1-1-1"),
                                    new NavLink(name: "3rd level link 2",title:"3rd level link 2", url: "http://msn.com", key: "key1-1-2",disabled:true)
                                }),
                            new NavLink(name: "Child link 2",title:"Child link 2", url: "http://msn.com", key: "key1-2"),
                            new NavLink(name: "Child link 3",title:"Child link 3", url: "http://msn.com", key: "key1-3",disabled:true),
                       }),
            new NavLink(name: "Parent link 2",
                        title: "Parent link 2",
                        url: "http://example.com",
                        key: "key2",
                        collapseAriaLabel: "Collapse Parent link 2",
                        links: new List<NavLink>
                        {
                            new NavLink(name: "Child link 4", title: "Child link 4", url: "http://example.com", key: "key2-1")
                        })
        };

        private readonly List<NavLink> CustomHeaderLinks = new()
        {
            new NavLink(name: "Pages",
                      links: new List<NavLink>
                      {
                            new NavLink(name: "Activity", url: "http://msn.com", key: "key1-1", title:"Activity"),
                            new NavLink(name: "News",title:"News", url: "http://msn.com", key: "key1-2"),
                      }),
            new NavLink(name: "More pages",
                       links: new List<NavLink>
                       {
                            new NavLink(name: "Settings", title: "Settings", url: "http://example.com", key: "key2-1"),
                            new NavLink(name: "Notes", title: "Notes", url: "http://example.com", key: "key2-1")
                       })
        };

        private void HideMessageBar(MouseEventArgs args)

        {
            IsMessageBarHidden = true;
        }
    }
}
