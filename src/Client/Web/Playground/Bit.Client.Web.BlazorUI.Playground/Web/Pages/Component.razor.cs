using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class Component
    {
        private bool CheckBoxOnChangedValue = false;
        private bool IsCheckBoxChecked = false;
        private bool IsCheckBoxIndeterminate = true;
        private bool IsCheckBoxIndeterminateInCode = true;
        private bool IsToggleChecked = true;
        private bool IsToggleUnChecked = false;

        private bool IsMessageBarHidden = false;
        private TextFieldType InputType = TextFieldType.Password;

        private int RatingBoundValue = 2;
        private int RatingLargeValue = 3;
        private int RatingSmallValue = 4;
        private int RatingReadOnlyValue = 5;
        private int RatingOutsideValue = 5;

        private readonly Uri UploadUrl = new("https://localhost:5001/FileUpload/UploadStreamedFile");
        private readonly Uri RemoveUrl = new("https://localhost:5001/FileUpload/RemoveFile");

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

        public string SelectedColor { get; set; } = "rgb(243,33,105,0.30)";

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
                Key = "key1",
                Title = "Home",
                Target = "_blank",
                IsExpanded = true,
                CollapseAriaLabel = "Collapse Home section",
                Links = new List<BitNavLinkItem>
                        {
                            new BitNavLinkItem{
                                Name= "Activity",
                                Url= "http://msn.com",
                                Key= "key1-1",
                                Title="Activity",
                                Links=new List<BitNavLinkItem>
                                {
                                    new BitNavLinkItem{ Name= "Activity",Title= "Activity",Url= "http://msn.com",Key= "key1-1-1"},
                                    new BitNavLinkItem{ Name= "MSN",Title= "MSN", Url= "http://msn.com", Key= "key1-1-2",Disabled=true}
                                }
                            },
                            new BitNavLinkItem{Name= "MSN",Title="MSN", Url= "http://msn.com", Key= "key1-2",Disabled=true },
                        }
            },
            new BitNavLinkItem { Name = "Documents", Title = "Documents", Url = "http://example.com", Key = "key2", Target = "_blank", IsExpanded = true },
            new BitNavLinkItem { Name = "Pages", Title = "Pages", Url = "http://msn.com", Key = "key3", Target = "_parent" },
            new BitNavLinkItem { Name = "Notebook", Title = "Notebook", Url = "http://msn.com", Key = "key4", Disabled = true },
            new BitNavLinkItem { Name = "Communication and Media", Title = "Communication and Media", Url = "http://msn.com", Key = "key5", Target = "_top" },
            new BitNavLinkItem { Name = "News", Title = "News", Url = "http://msn.com", Key = "key6", Icon = "News", Target = "_self" },
        };

        private readonly List<BitNavLinkItem> BasicNoToolTipNavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Home",
                Url = "http://example.com",
                Key = "key1",
                Target = "_blank",
                IsExpanded = true,
                CollapseAriaLabel = "Collapse Home section",
                Links = new List<BitNavLinkItem>
                        {
                            new BitNavLinkItem{
                                Name= "Activity",
                                Url= "http://msn.com",
                                Key= "key1-1",
                                Links=new List<BitNavLinkItem>
                                {
                                    new BitNavLinkItem{Name= "Activity",Url= "http://msn.com",Key= "key1-1-1" },
                                    new BitNavLinkItem{Name= "MSN", Url= "http://msn.com", Key= "key1-1-2",Disabled=true }
                                } },
                            new BitNavLinkItem{Name= "MSN", Url= "http://msn.com", Key= "key1-2",Disabled=true },
                        }
            },
            new BitNavLinkItem { Name = "Shared Documents and Files", Url = "http://example.com", Key = "key2", Target = "_blank", IsExpanded = true },
            new BitNavLinkItem { Name = "Pages", Url = "http://msn.com", Key = "key3", Target = "_parent" },
            new BitNavLinkItem { Name = "Notebook", Url = "http://msn.com", Key = "key4", Disabled = true },
            new BitNavLinkItem { Name = "Communication and Media", Url = "http://msn.com", Key = "key5", Target = "_top" },
            new BitNavLinkItem { Name = "News", Url = "http://msn.com", Key = "key6", Icon = "News", Target = "_self" },
        };

        private readonly List<BitNavLinkItem> BasicNoUrlNavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Basic components",
                Key = "Key1",
                CollapseAriaLabel = "Collapse Basic components section",
                IsExpanded = true,
                Links = new List<BitNavLinkItem>
                        {
                            new BitNavLinkItem{Name= "ActivityItem", Key= "ActivityItem",Url="#/examples/activityitem" },
                            new BitNavLinkItem{Name= "Breadcrumb", Key= "Breadcrumb",Url="#/examples/breadcrumb" },
                            new BitNavLinkItem{Name= "Button", Key= "Button",Url="#/examples/button" }
                        }
            },
            new BitNavLinkItem
            {
                Name = "Extended components",
                Key = "Key2",
                CollapseAriaLabel = "Collapse Extended components section",
                IsExpanded = true,
                Links = new List<BitNavLinkItem>
                        {
                            new BitNavLinkItem{Name= "ColorPicker", Key= "ColorPicker",Url="#/examples/colorpicker" },
                            new BitNavLinkItem{Name= "ExtendedPeoplePicker", Key= "ExtendedPeoplePicker",Url="#/examples/extendedpeoplepicker" },
                            new BitNavLinkItem{Name= "GroupedList", Key= "GroupedList",Url="#/examples/groupedlist" }
                        }
            },
            new BitNavLinkItem
            {
                Name = "Utilities",
                Key = "Key3",
                CollapseAriaLabel = "Collapse Utilities section",
                IsExpanded = true,
                Links = new List<BitNavLinkItem>
                        {
                            new BitNavLinkItem{Name= "FocusTrapZone", Key= "FocusTrapZone",Url="#/examples/focustrapzone" },
                            new BitNavLinkItem{Name= "FocusZone", Key= "FocusZone",Url="#/examples/focuszone" },
                            new BitNavLinkItem{Name= "MarqueeSelection", Key= "MarqueeSelection",Url="#/examples/marqueeselection" }
                        }
            },
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
                        Name= "Child link 1",
                        Url= "http://msn.com",
                        Key= "Key1-1",
                        Title="Child link 1",
                        Links=new List<BitNavLinkItem>
                        {
                            new BitNavLinkItem{Name= "3rd level link 1",Title="3rd level link 1",Url= "http://msn.com",Key= "Key1-1-1" },
                            new BitNavLinkItem{Name= "3rd level link 2",Title="3rd level link 2", Url= "http://msn.com", Key= "Key1-1-2",Disabled=true }
                        }
                    },
                    new BitNavLinkItem{Name = "Child link 2", Title = "Child link 2", Url = "http://msn.com", Key = "Key1-2" },
                    new BitNavLinkItem{Name = "Child link 3", Title = "Child link 3", Url = "http://msn.com", Key = "Key1-3", Disabled = true },
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
                    new BitNavLinkItem{Name= "Child link 4", Title= "Child link 4", Url= "http://example.com", Key= "Key2-1" }
                }
            }
        };

        private readonly List<BitNavLinkItem> CustomHeaderLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Pages",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem{Name= "Activity", Url= "http://msn.com", Key= "Key1-1", Title="Activity" },
                    new BitNavLinkItem{Name= "News",Title="News", Url= "http://msn.com", Key= "Key1-2" },
                }
            },
            new BitNavLinkItem
            {
                Name = "More pages",
                Links = new List<BitNavLinkItem>
                {
                    new BitNavLinkItem{Name= "Settings", Title= "Settings", Url= "http://example.com", Key= "Key2-1" },
                    new BitNavLinkItem{Name= "Notes", Title= "Notes", Url= "http://example.com", Key= "Key2-1" }
                }
            }
        };

        #region BitDetailsList properties
        private List<BitDocument> DetailListItems { get; set; } = new();

        private static readonly string[] FileIcons =
        {
            "accdb",
            "audio",
            "code",
            "csv",
            "docx",
            "dotx",
            "mpp",
            "mpt",
            "model",
            "one",
            "onetoc",
            "potx",
            "ppsx",
            "pdf",
            "photo",
            "pptx",
            "presentation",
            "potx",
            "pub",
            "rtf",
            "spreadsheet",
            "txt",
            "vector",
            "vsdx",
            "vssx",
            "vstx",
            "xlsx",
            "xltx",
            "xsn",
        };

        private static readonly string LoremIpsumStr =
            "lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor incididunt ut " +
            "labore et dolore magna aliqua ut enim ad minim veniam quis nostrud exercitation ullamco laboris nisi ut " +
            "aliquip ex ea commodo consequat duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore " +
            "eu fugiat nulla pariatur excepteur sint occaecat cupidatat non proident sunt in culpa qui officia deserunt";
        private static readonly List<string> LoremIpsum = LoremIpsumStr.Split(" ").ToList(); 
        #endregion

        #region PivotSamples

        public string OverridePivotSelectedKey { get; set; } = "1";
        public string SelectedPivotItemKey { get; set; } = "1";
        public BitPivotItem BitPivotItem { get; set; }
        public ComponentVisibility PivotItemVisibility { get; set; }
        public BitPivotItem SelectedPivotKey { get; set; } = new BitPivotItem { ItemKey = "Foo" };

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

            DetailListItems = GenerateDocument();
        }

        private static List<DropDownItem> GetDropdownItems()
        {
            List<DropDownItem> items = new();
            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Header,
                Text = "Fruits"
            });
            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Normal,
                Text = "Apple",
                Value = "f-app"
            });
            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Normal,
                Text = "Orange",
                Value = "f-ora",
                IsEnabled = false
            });
            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Normal,
                Text = "Banana",
                Value = "f-ban",
            });
            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Divider,
            });
            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Header,
                Text = "Vegetables"
            });
            items.Add(new DropDownItem()
            {
                ItemType = DropDownItemType.Normal,
                Text = "Broccoli",
                Value = "v-bro",
            });

            return items;
        }

        #region BitDetailsList methods

        private static List<BitDocument> GenerateDocument()
        {
            List<BitDocument> result = new();
            for (int i = 0; i < 500; i++)
            {
                var range = FileIcons.Length - 1;
                var random = new Random();
                var randomNumber = random.Next(0, range);
                var fileSize = (randomNumber == 0 ? 1 : randomNumber) * 1000;

                var docType = FileIcons[randomNumber];
                var iconUrl = $"https://static2.sharepointonline.com/files/fabric/assets/item-types/16/{docType}.svg";
                var randomDate = RandomDate();
                string fileName = Lorem(2);
                string userName = Lorem(2);
                BitDocument bitDocument = new()
                {
                    Key = i.ToString(),
                    DateModified = randomDate.ToString("G"),
                    DateModifiedValue = randomDate,
                    FileSize = fileSize,
                    FileType = docType,
                    IconName = iconUrl,
                    ModifiedBy = userName,
                    Name = fileName,
                    Value = fileName
                };

                result.Add(bitDocument);
            }

            return result;
        }

        private static string Lorem(int wordCount)
        {
            var maxRange = wordCount > LoremIpsum.Count ? LoremIpsum.Count : wordCount;
            var range = LoremIpsum.Count - maxRange - 1;
            var random = new Random();
            var loremIndex = random.Next(0, range);
            int startIndex = (loremIndex + wordCount) > LoremIpsum.Count ? 0 : loremIndex;
            loremIndex = startIndex + wordCount;

            var value = "";
            for (int i = startIndex; i < loremIndex; i++)
            {
                value += $"{LoremIpsum[i]} ";
            }
            return value.Trim();
        } 
        #endregion


        private static DateTimeOffset RandomDate()
        {
            var random = new Random();
            DateTime start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;
            return start.AddDays(random.Next(range));
        }
        private string? TextValue;
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
}
