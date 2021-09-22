using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class Component
    {
        private bool IsCheckBoxChecked = true;
        private bool IsCheckBoxIndeterminate = true;
        private bool IsCheckBoxIndeterminateInCode = true;

        private void HandleIsCheckedChange(bool isChecked)
        {
            IsCheckBoxChecked = isChecked;
        }

        private bool IsToggleChecked = true;
        private bool IsToggleUnChecked = false;

        private bool IsMessageBarHidden = false;
        private TextFieldType InputType = TextFieldType.Password;

        private int RatingBoundValue = 2;
        private int RatingLargeValue = 3;
        private int RatingSmallValue = 4;
        private int RatingReadOnlyValue = 5;
        private int RatingOutsideValue = 5;

        private readonly string UploadUrl = "/FileUpload/UploadStreamedFile";
        private readonly string RemoveUrl = "/FileUpload/RemoveFile";

        private double BasicSpinButtonValue = 5;
        private double BasicSpinButtonDisableValue = 20;
        private double SpinButtonWithCustomHandlerValue = 14;
        private double SpinButtonWithLabelAboveValue = 7;

        public int CompletedPercent { get; set; }
        private string description = "Push button to start !";
        private async Task StartProgress()
        {
            CompletedPercent = 0;
            while (CompletedPercent <= 100)
            {
                if (CompletedPercent == 100)
                {
                    description = $"Completed !";
                    break;
                }
                else
                {
                    CompletedPercent++;
                    description = $"{CompletedPercent}%";
                }
                StateHasChanged();
                await Task.Delay(100);
            }
        }

        public string SelectedColor { get; set; } = "#ffeeff";

        private void HandleSpinButtonValueChange(double value)
        {
            SpinButtonWithCustomHandlerValue = value;
        }

        private readonly List<BitNavLinkItem> BasicNavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Home",
                Url = "http://example.com",
                ExpandAriaLabel = "Expand Home section",
                CollapseAriaLabel = "Collapse Home section",
                IsExpanded = true,
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "Activity", Url = "http://msn.com", Key = "key1", Target="_blank" },
                    new BitNavLinkItem { Name = "MSN", Url = "http://msn.com", Key = "key2", IsEnabled = false, Target = "_blank" }
                }
            },
            new BitNavLinkItem { Name = "Documents", Url = "http://example.com", Key = "key3", Target = "_blank", IsExpanded = true },
            new BitNavLinkItem { Name = "Pages", Url = "http://msn.com", Key = "key4", Target = "_parent" },
            new BitNavLinkItem { Name = "Notebook", Url = "http://msn.com", Key = "key5", IsEnabled = false },
            new BitNavLinkItem { Name = "Communication and Media", Url = "http://msn.com", Key = "key6", Target = "_top" },
            new BitNavLinkItem { Name = "News", Title = "News", Url = "http://msn.com", Key = "key7", Icon = "News", Target = "_self" },
        };

        private readonly List<BitNavLinkItem> BasicNoToolTipNavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Home",
                Url = "http://example.com",
                Title = "",
                IsExpanded = true,
                CollapseAriaLabel = "Collapse Home section",
                ExpandAriaLabel = "Expand Home section",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "Activity", Url = "http://msn.com", Key = "key1", Target="_blank", Title = "" },
                    new BitNavLinkItem { Name = "MSN", Url = "http://msn.com", Key = "key2", IsEnabled = false, Target = "_blank", Title = "" }
                }
            },
            new BitNavLinkItem { Name = "Shared Documents and Files", Url = "http://example.com", Key = "key3", Target = "_blank", Title = "" },
            new BitNavLinkItem { Name = "Pages", Url = "http://msn.com", Key = "key4", Target = "_parent", Title = "" },
            new BitNavLinkItem { Name = "Notebook", Url = "http://msn.com", Key = "key5", IsEnabled = false, Title = "" },
            new BitNavLinkItem { Name = "Communication and Media", Url = "http://msn.com", Key = "key6", Target = "_top", Title = "" },
            new BitNavLinkItem { Name = "News", Key = "key7", Url = "http://cnn.com", Icon = "News", Target = "_self", Title = "" }
        };

        private readonly List<BitNavLinkItem> BasicNoUrlNavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Basic components",
                CollapseAriaLabel = "Collapse Basic components section",
                IsExpanded = true,
                IsGroup = true,
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name= "ActivityItem", Key = "ActivityItem", Url = "#/examples/activityitem" },
                    new BitNavLinkItem { Name= "Breadcrumb", Key = "Breadcrumb", Url = "#/examples/breadcrumb" },
                    new BitNavLinkItem { Name= "Button", Key = "Button", Url = "#/examples/button" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Extended components",
                CollapseAriaLabel = "Collapse Extended components section",
                IsExpanded = true,
                IsGroup = true,
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "ColorPicker", Key = "ColorPicker", Url ="#/examples/colorpicker" },
                    new BitNavLinkItem { Name = "ExtendedPeoplePicker", Key = "ExtendedPeoplePicker", Url ="#/examples/extendedpeoplepicker" },
                    new BitNavLinkItem { Name = "GroupedList", Key = "GroupedList", Url ="#/examples/groupedlist" }
                }
            },
            new BitNavLinkItem
            {
                Name = "Utilities",
                CollapseAriaLabel = "Collapse Utilities section",
                IsExpanded = true,
                IsGroup = true,
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "FocusTrapZone", Key = "FocusTrapZone", Url = "#/examples/focustrapzone" },
                    new BitNavLinkItem { Name = "FocusZone", Key = "FocusZone", Url = "#/examples/focuszone" },
                    new BitNavLinkItem { Name = "MarqueeSelection", Key = "MarqueeSelection", Url = "#/examples/marqueeselection" }
                }
            }
        };

        private readonly List<BitNavLinkItem> NestedLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Parent link 1",
                Url = "http://example.com",
                Key = "Key1",
                Title = "Parent link 1",
                CollapseAriaLabel = "Collapse Parent link 1",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem
                    {
                        Name = "Child link 1",
                        Url = "http://msn.com",
                        Key = "Key1-1",
                        Title = "Child link 1",
                        Links = new List<BitNavLinkItem>
                        {
                            new BitNavLinkItem { Name = "3rd level link 1", Title = "3rd level link 1", Url = "http://msn.com", Key = "Key1-1-1" },
                            new BitNavLinkItem { Name = "3rd level link 2", Title = "3rd level link 2", Url = "http://msn.com", Key = "Key1-1-2", IsEnabled = false }
                        }
                    },
                    new BitNavLinkItem { Name = "Child link 2", Title = "Child link 2", Url = "http://msn.com", Key = "Key1-2" },
                    new BitNavLinkItem { Name = "Child link 3", Title = "Child link 3", Url = "http://msn.com", Key = "Key1-3", IsEnabled = false },
                }
            },
            new BitNavLinkItem
            {
                Name = "Parent link 2",
                Title = "Parent link 2",
                Url = "http://example.com",
                Key = "Key2",
                CollapseAriaLabel = "Collapse Parent link 2",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "Child link 4", Title = "Child link 4", Url = "http://example.com", Key = "Key2-1" }
                }
            }
        };

        private readonly List<BitNavLinkItem> CustomHeaderLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Pages",
                IsGroup = true,
                IsExpanded = true,
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name = "Activity", Url = "http://msn.com", Key = "Key1-1", Title = "Activity" },
                    new BitNavLinkItem { Name = "News", Title = "News", Url = "http://msn.com", Key = "Key1-2" },
                }
            },
            new BitNavLinkItem
            {
                Name = "More pages",
                IsGroup = true,
                IsExpanded = true,
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem { Name= "Settings", Title = "Settings", Url = "http://example.com", Key = "Key2-1" },
                    new BitNavLinkItem { Name= "Notes", Title = "Notes", Url = "http://example.com", Key = "Key2-1" }
                }
            }
        };

        #region PivotSamples

        public string OverridePivotSelectedKey { get; set; } = "1";
        public BitPivotItem BitPivotItem { get; set; }
        public ComponentVisibility PivotItemVisibility { get; set; }
        public string SelectedPivotItemKey { get; set; } = "Foo";

        public void PivotSelectedKeyChanged(string key)
        {
            OverridePivotSelectedKey = key;
        }

        public void TogglePivotItemVisobility()
        {
            PivotItemVisibility = PivotItemVisibility == ComponentVisibility.Visible ? ComponentVisibility.Collapsed : ComponentVisibility.Visible;
        }

        #endregion 

        private bool TogleButtonChecked = true;
        private bool TogleButtonChecked2 = true;
        private bool TogleButtonTwoWayValue = true;
        private bool TogleButtonTwoWayValue2 = true;
        private bool OnToggleButtonChanged;
        private void ToggleButtonChanged(bool newValue)
        {
            OnToggleButtonChanged = newValue;
        }

        private void HideMessageBar()
        {
            IsMessageBarHidden = true;
        }
        private string RatingChangedText = "";

        private List<Person>[] people = new List<Person>[2];

        protected override void OnInitialized()
        {
            base.OnInitialized();

            //initialize people list
            Person person = new Person();
            people[0] = person.GetPeople(8000);
            people[1] = person.GetPeople(100);
        }

        private List<BitDropDownItem> GetDropdownItems()
        {
            List<BitDropDownItem> items = new();
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Fruits"
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
                IsSelected = true
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Divider,
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Vegetables"
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            });

            return items;
        }

        private List<BitDropDownItem> GetCustomDropdownItems()
        {
            List<BitDropDownItem> items = new();
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "Options",
                Value = "Header"
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option a",
                Value = "A",
                Data = new DropDownItemData()
                {
                    IconName = "Memo"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option b",
                Value = "B",
                Data = new DropDownItemData()
                {
                    IconName = "Print"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option c",
                Value = "C",
                Data = new DropDownItemData()
                {
                    IconName = "ShoppingCart"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option d",
                Value = "D",
                Data = new DropDownItemData()
                {
                    IconName = "Train"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option e",
                Value = "E",
                Data = new DropDownItemData()
                {
                    IconName = "Repair"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Divider
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Header,
                Text = "More options",
                Value = "Header2"
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option f",
                Value = "F",
                Data = new DropDownItemData()
                {
                    IconName = "Running"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option g",
                Value = "G",
                Data = new DropDownItemData()
                {
                    IconName = "EmojiNeutral"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option h",
                Value = "H",
                Data = new DropDownItemData()
                {
                    IconName = "ChatInviteFriend"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option i",
                Value = "I",
                Data = new DropDownItemData()
                {
                    IconName = "SecurityGroup"
                }
            });
            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Option j",
                Value = "J",
                Data = new DropDownItemData()
                {
                    IconName = "AddGroup"
                }
            });

            return items;
        }

        private string? TextValue;
        private string? SelectedDate;
        private double? BitSliderHorizontalValue = 2;
        private double? BitSliderVerticalValue = 0;
        private double? BitSliderRangedLowerValue = 0;
        private double? BitSliderRangedUpperValue = 0;

        private void ChangeBitSliderRangedValues()
        {
            BitSliderRangedLowerValue = 2;
            BitSliderRangedUpperValue = 9;
        }

        private string OnDateFormat(BitDate date)
        {
            return $"{date.GetDate()}/{date.GetMonth()}/{date.GetYear()}";
        }
    }

    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Job { get; set; }

        public List<Person> GetPeople(int itemCount)
        {
            List<Person> people = new();
            for (int i = 0; i < itemCount; i++)
            {
                people.Add(new Person
                {
                    Id = i + 1,
                    FirstName = $"Person {(i + 1).ToString()}",
                    LastName = $"Person Family {(i + 1).ToString()}",
                    Job = $"Programmer {(i + 1).ToString()}"
                });
            }
            return people;
        }
    }

    public class DropDownItemData
    {
        public string IconName { get; set; }
    }
}
