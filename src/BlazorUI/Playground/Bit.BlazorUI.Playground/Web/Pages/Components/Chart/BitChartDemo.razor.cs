using System;
using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Chart;

public partial class BitChartDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
         new ComponentParameter
        {
            Name = "SetupCompletedCallback",
            Type = "EventCallback",
            DefaultValue = "",
            Description = @"This event is fired when the chart has been setup through interop and                            the JavaScript chart object is available. Use this callback if you need to setup custom JavaScript options or register plugins.",
        },

         new ComponentParameter
        {
            Name = "Config",
            Type = "BitChartConfigBase",
            DefaultValue = "",
            Description = "Gets or sets the configuration of the chart.",
        },

         new ComponentParameter
        {
            Name = "Width",
            Type = "int?",
            DefaultValue = "",
            Description = "Gets or sets the width of the canvas HTML element.",
        },

         new ComponentParameter
        {
            Name = "Height",
            Type = "int?",
            DefaultValue = "",
            Description = @"Gets or sets the height of the canvas HTML element. Use <see langword=""null""/> when using <see cref=""BitChartBaseConfigOptions.AspectRatio""/>.",
        }
    };

    #region Bar Chart
    private readonly string example1HTMLCode = @"
<div>
    <BitChart Config=""_barChartConfigExample"" @ref=""_barChartExample"" />
</div>
<div>
    <BitButton ButtonStyle = ""BitButtonStyle.Primary"" OnClick=""RandomizeBarData"">Randomize Data</BitButton>
    <BitButton ButtonStyle = ""BitButtonStyle.Standard"" OnClick= ""AddBarDataset"" > Add Dataset</BitButton>
    <BitButton ButtonStyle = ""BitButtonStyle.Standard"" OnClick= ""RemoveBarDataset"" > Remove Dataset</BitButton>
    <BitButton ButtonStyle = ""BitButtonStyle.Standard"" OnClick= ""AddBarData"" > Add Data</BitButton>
    <BitButton ButtonStyle = ""BitButtonStyle.Standard"" OnClick= ""RemoveBarData"" > Remove Data</BitButton>
</div>";
    private readonly string example1CSharpCode = @"
private const int InitalCount = 5;
private BitChartBarConfig _barChartConfigExample;
private BitChart _barChartExample;

protected override void OnInitialized()
{
    InitBarChartExample();
}

private void InitBarChartExample()
{
    _barChartConfigExample = new BitChartBarConfig
        {
            Options = new BitChartBarOptions()
            {
                Responsive = true,
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = ""BitChart bar Chart""
                }
            }
        };
    BitChartBarDataset<int> dataset = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount))
    {
        BackgroundColor = BitChartDemoUtils.ChartColors.All.Take(InitalCount).Select(c => BitChartColorUtil.FromDrawingColor(Color.FromArgb(128, c))).ToArray()
    };
    _barChartConfigExample.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(InitalCount));
    _barChartConfigExample.Data.Datasets.Add(dataset);
}

private void RandomizeBarData()
{
    foreach (IDataset<int> dataset in _barChartConfigExample.Data.Datasets)
    {
        int count = dataset.Count;
        dataset.Clear();
        for (int i = 0; i < count; i++)
        {
            if (BitChartDemoUtils._rng.NextDouble() < 0.2)
            {
                dataset.Add(0);
            }
            else
            {
                dataset.Add(BitChartDemoUtils.RandomScalingFactor());
            }
        }
    }

    _barChartExample.Update();
}

private void AddBarDataset()
{
    Color color = BitChartDemoUtils.ChartColors.All[_barChartConfigExample.Data.Datasets.Count % BitChartDemoUtils.ChartColors.All.Count];
    IDataset<int> dataset = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(_barChartConfigExample.Data.Labels.Count))
        {
            Label = $""Dataset {_barChartConfigExample.Data.Datasets.Count + 1}"",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(Color.FromArgb(128, color)),
            BorderColor = BitChartColorUtil.FromDrawingColor(color),
            BorderWidth = 1
        };

    _barChartConfigExample.Data.Datasets.Add(dataset);
    _barChartExample.Update();
}

private void RemoveBarDataset()
{
    IList<IBitChartDataset> datasets = _barChartConfigExample.Data.Datasets;
    if (datasets.Count == 0)
        return;

    datasets.RemoveAt(datasets.Count - 1);
    _barChartExample.Update();
}

private void AddBarData()
{
    if (_barChartConfigExample.Data.Datasets.Count == 0)
        return;

    string month = BitChartDemoUtils.Months[_barChartConfigExample.Data.Labels.Count % BitChartDemoUtils.Months.Count];
    _barChartConfigExample.Data.Labels.Add(month);

    foreach (IDataset<int> dataset in _barChartConfigExample.Data.Datasets)
    {
        dataset.Add(BitChartDemoUtils.RandomScalingFactor());
    }

    _barChartExample.Update();
}

private void RemoveBarData()
{
    if (_barChartConfigExample.Data.Datasets.Count == 0 || _barChartConfigExample.Data.Labels.Count == 0)
    {
        return;
    }

    _barChartConfigExample.Data.Labels.RemoveAt(_barChartConfigExample.Data.Labels.Count - 1);

    foreach (IDataset<int> dataset in _barChartConfigExample.Data.Datasets)
    {
        dataset.RemoveAt(dataset.Count - 1);
    }

    _barChartExample.Update();
}
";
    #endregion

    #region Horizontal Bar Chart
    private readonly string example2HTMLCode = @"
<div>
    <BitChart Config=""_horizontalBarChartConfigExample"" @ref=""_horizontalBarChartExample"" />
</div>
";
    private readonly string example2CSharpCode = @"
private BitChartBarConfig _horizontalBarChartConfigExample;
private BitChart _horizontalBarChartExample;

protected override void OnInitialized()
{
    InitHorizontalBarChartExample();
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
                    Text = ""BitChart Horizontal Bar Chart""
                }
            }
        };

    IDataset<int> dataset1 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount, -100), horizontal: true)
        {
            Label = ""My first dataset"",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(Color.FromArgb(128, BitChartDemoUtils.ChartColors.Red)),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoUtils.ChartColors.Red),
            BorderWidth = 1
        };

    IDataset<int> dataset2 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount, -100), horizontal: true)
        {
            Label = ""My second dataset"",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(Color.FromArgb(128, BitChartDemoUtils.ChartColors.Blue)),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoUtils.ChartColors.Blue),
            BorderWidth = 1
        };

    _horizontalBarChartConfigExample.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(InitalCount));
    _horizontalBarChartConfigExample.Data.Datasets.Add(dataset1);
    _horizontalBarChartConfigExample.Data.Datasets.Add(dataset2);
}
";
    #endregion

    #region Stacked Bar Chart
    private readonly string example3HTMLCode = @"
<div>
    <BitChart Config=""_stackedBarChartConfigExample"" @ref=""_stackedBarChartExample"" />
</div>
";
    private readonly string example3CSharpCode = @"
private BitChartBarConfig _stackedBarChartConfigExample;
private BitChart _stackedBarChartExample;

protected override void OnInitialized()
{
    InitStackedBarChartExample();
}

private void InitStackedBarChartExample()
{
    _stackedBarChartConfigExample = new BitChartBarConfig
    {
        Options = new BitChartBarOptions()
        {
            Responsive = true,
            Title = new BitChartOptionsTitle
            {
                Display = true,
                Text = ""BitChart stacked bar Chart""
            },
            Tooltips = new BitChartTooltips
            {
                Mode = BitChartInteractionMode.Index,
                Intersect = false
            },
            Scales = new BitChartBarScales
            {
                XAxes = new List<BitChartCartesianAxis>
            {
                new BitChartBarCategoryAxis
                {
                    Stacked = true
                }
            },
                YAxes = new List<BitChartCartesianAxis>
            {
                new BitChartBarLinearCartesianAxis
                {
                    Stacked = true
                }
            }
            }
        }
    };

    IDataset<int> dataset1 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount))
    {
        Label = ""Dataset 1"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoUtils.ChartColors.Red)
    };

    IDataset<int> dataset2 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount))
    {
        Label = ""Dataset 2"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoUtils.ChartColors.Blue)
    };

    IDataset<int> dataset3 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount))
    {
        Label = ""Dataset 3"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoUtils.ChartColors.Green)
    };


    _stackedBarChartConfigExample.Data.Datasets.Add(dataset1);
    _stackedBarChartConfigExample.Data.Datasets.Add(dataset2);
    _stackedBarChartConfigExample.Data.Datasets.Add(dataset3);
    _stackedBarChartConfigExample.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(InitalCount));
}
";
    #endregion

    #region line Chart

    private readonly string example4HTMLCode = @"
<div>
    <BitChart Config=""_lineChartConfigExample"" @ref=""_lineChartExample"" />
</div>
";
    private readonly string example4CSharpCode = @"
private BitChartLineConfig _lineChartConfigExample;
private BitChart _lineChartExample;

protected override void OnInitialized()
{
    InitlineChartExample();
}

private void InitlineChartExample()
{
    _lineChartConfigExample = new BitChartLineConfig
        {
            Options = new BitChartLineOptions
            {
                Responsive = true,
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = ""BitChart Line Chart""
                },
                Tooltips = new BitChartTooltips
                {
                    Mode = BitChartInteractionMode.Nearest,
                    Intersect = true
                },
                Hover = new BitChartHover
                {
                    Mode = BitChartInteractionMode.Nearest,
                    Intersect = true
                },
                Scales = new BitChartScales
                {
                    XAxes = new List<BitChartCartesianAxis>
                {
                    new BitChartCategoryAxis
                    {
                        ScaleLabel = new BitChartScaleLabel
                        {
                            LabelString = ""Month""
                        }
                    }
                },
                    YAxes = new List<BitChartCartesianAxis>
                {
                    new BitChartLinearCartesianAxis
                    {
                        ScaleLabel = new BitChartScaleLabel
                        {
                            LabelString = ""Value""
                        }
                    }
                }
                }
            }
        };

    IDataset<int> dataset1 = new BitChartLineDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount))
        {
            Label = ""My first dataset"",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoUtils.ChartColors.Red),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoUtils.ChartColors.Red),
            Fill = BitChartFillingMode.Disabled
        };

    IDataset<int> dataset2 = new BitChartLineDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount))
        {
            Label = ""My second dataset"",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoUtils.ChartColors.Blue),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoUtils.ChartColors.Blue),
            Fill = BitChartFillingMode.Disabled
        };

    _lineChartConfigExample.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(InitalCount));
    _lineChartConfigExample.Data.Datasets.Add(dataset1);
    _lineChartConfigExample.Data.Datasets.Add(dataset2);
}
";
    #endregion

    #region Pie Chart
    private readonly string example5HTMLCode = @"
";
    private readonly string example5CSharpCode = @"
private BitChartPieConfig _pieChartConfigExample;
private BitChart _pieChartExample;

protected override void OnInitialized()
{
    InitPieChartExample();
}

private void InitPieChartExample()
{
    _pieChartConfigExample = new BitChartPieConfig
        {
            Options = new BitChartPieOptions
            {
                Responsive = true,
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = ""BitChart Pie Chart""
                }
            }
        };

    BitChartPieDataset<int> dataset = new BitChartPieDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount))
    {
        BackgroundColor = BitChartDemoUtils.ChartColors.All.Take(InitalCount).Select(c => BitChartColorUtil.FromDrawingColor(Color.FromArgb(220, c))).ToArray()
    };
    _pieChartConfigExample.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(InitalCount));
    _pieChartConfigExample.Data.Datasets.Add(dataset);
}

private void RandomizePieData()
{
    foreach (IDataset<int> dataset in _pieChartConfigExample.Data.Datasets)
    {
        int count = dataset.Count;
        dataset.Clear();
        for (int i = 0; i < count; i++)
        {
            if (BitChartDemoUtils._rng.NextDouble() < 0.2)
            {
                dataset.Add(0);
            }
            else
            {
                dataset.Add(BitChartDemoUtils.RandomScalingFactor());
            }
        }
    }

    _pieChartExample.Update();
}

private void AddPieDataset()
{
    int count = _pieChartConfigExample.Data.Labels.Count;
    BitChartPieDataset<int> dataset = new BitChartPieDataset<int>(BitChartDemoUtils.RandomScalingFactor(count, -100, 100))
        {
            BackgroundColor = BitChartDemoUtils.ChartColors.All.Take(count).Select(BitChartColorUtil.FromDrawingColor).ToArray()
        };

    _pieChartConfigExample.Data.Datasets.Add(dataset);
    _pieChartExample.Update();
}

private void RemovePieDataset()
{
    IList<IBitChartDataset> datasets = _pieChartConfigExample.Data.Datasets;
    if (datasets.Count == 0)
        return;

    datasets.RemoveAt(0);
    _pieChartExample.Update();
}

private void AddPieData()
{
    if (_pieChartConfigExample.Data.Datasets.Count == 0)
        return;

    string month = BitChartDemoUtils.Months[_pieChartConfigExample.Data.Labels.Count % BitChartDemoUtils.Months.Count];
    _pieChartConfigExample.Data.Labels.Add(month);

    foreach (IDataset<int> dataset in _pieChartConfigExample.Data.Datasets)
    {
        dataset.Add(BitChartDemoUtils.RandomScalingFactor());
    }

    _pieChartExample.Update();
}

private void RemovePieData()
{
    if (_pieChartConfigExample.Data.Datasets.Count == 0 ||
        _pieChartConfigExample.Data.Labels.Count == 0)
    {
        return;
    }

    _pieChartConfigExample.Data.Labels.RemoveAt(_pieChartConfigExample.Data.Labels.Count - 1);

    foreach (IDataset<int> dataset in _pieChartConfigExample.Data.Datasets)
    {
        dataset.RemoveAt(dataset.Count - 1);
    }

    _pieChartExample.Update();
}
";
    #endregion

    #region Doughnut Chart
    private readonly string example6HTMLCode = @"
<div>
    <BitChart Config=""_doughnutChartConfigExample"" @ref=""_doughnutChartExample"" />
</div>
";
    private readonly string example6CSharpCode = @"
private BitChartPieConfig _doughnutChartConfigExample;
private BitChart _doughnutChartExample;

protected override void OnInitialized()
{
    InitDoughnutChartExample();
}

private void InitDoughnutChartExample()
{
    _doughnutChartConfigExample = new BitChartPieConfig(useDoughnutType:true)
        {
            Options = new BitChartPieOptions
            {
                Responsive = true,
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = ""BitChart Doughnut Chart""
                }
            }
        };

    BitChartPieDataset<int> dataset = new BitChartPieDataset<int>(BitChartDemoUtils.RandomScalingFactor(InitalCount))
        {
            BackgroundColor = BitChartDemoUtils.ChartColors.All.Take(InitalCount).Select(c => BitChartColorUtil.FromDrawingColor(Color.FromArgb(220, c))).ToArray()
        };
    _doughnutChartConfigExample.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(InitalCount));
    _doughnutChartConfigExample.Data.Datasets.Add(dataset);
}
";
    #endregion
}
