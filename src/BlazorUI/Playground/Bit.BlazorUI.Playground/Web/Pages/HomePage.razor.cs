using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bit.BlazorUI.Playground.Web.Pages.Components.Chart;

namespace Bit.BlazorUI.Playground.Web.Pages;

public partial class HomePage
{
    public string OnClickValue { get; set; } = string.Empty;
    private string SelectedColor = "#0065EF";
    private BitChartBarConfig _horizontalBarChartConfigExample;
    private BitChart _horizontalBarChartExample;
    private const int InitalCount = 5;
    private bool IsToggleChecked = true;
    private bool IsToggleUnChecked = false;

    protected override void OnInitialized()
    {
        InitHorizontalBarChartExample();
        base.OnInitialized();
    }

    private void InitHorizontalBarChartExample()
    {
        _horizontalBarChartConfigExample = new BitChartBarConfig(horizontal: true)
        {
            Options = new BitChartBarOptions
            {
                Responsive = true,
                Legend = new BitChartLegend
                {
                    Position = BitChartPosition.Right
                },
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = "BitChart Horizontal Bar Chart"
                }
            }
        };

        IDataset<int> dataset1 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount, -100), horizontal: true)
        {
            Label = "My first dataset",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(Color.FromArgb(128, BitChartDemoColors.Red)),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red),
            BorderWidth = 1
        };

        IDataset<int> dataset2 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount, -100), horizontal: true)
        {
            Label = "My second dataset",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(Color.FromArgb(128, BitChartDemoColors.Blue)),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue),
            BorderWidth = 1
        };

        _horizontalBarChartConfigExample.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(InitalCount));
        _horizontalBarChartConfigExample.Data.Datasets.Add(dataset1);
        _horizontalBarChartConfigExample.Data.Datasets.Add(dataset2);
    }

    private List<BitBreadcrumbItem> GetBreadcrumbItems()
    {
        return new List<BitBreadcrumbItem>()
         {
             new()
             {
                 Text = "Folder 1",
                 Key = "f1",
                 href = "/components/breadcrumb",
                 OnClick = (() => OnClickValue = "Folder 1 clicked")
             },
             new()
             {
                 Text = "Folder 2",
                 Key = "f2",
                 href = "/components/breadcrumb",
                 OnClick = (() => OnClickValue = "Folder 2 clicked")
             },
             new()
             {
                 Text = "Folder 3",
                 Key = "f3",
                 href = "/components/breadcrumb",
                 OnClick = (() => OnClickValue = "Folder 3 clicked")
             },
             new()
             {
                 Text = "Folder 4",
                 Key = "f4",
                 href = "/components/breadcrumb",
                 IsCurrentItem = true,
                 OnClick = (() => OnClickValue = "Folder 4 clicked")
             }
         };
    }

    private List<BitDropDownItem> GetProductDropdownItems()
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
}
