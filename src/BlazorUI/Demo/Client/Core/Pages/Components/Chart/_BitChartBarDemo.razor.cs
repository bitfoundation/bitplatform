namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Chart;

public partial class _BitChartBarDemo
{
    private const int INITAL_COUNT = 5;

    private BitChart _chart = default!;
    private BitChartBarConfig _config = default!;

    protected override void OnInitialized()
    {
        _config = new BitChartBarConfig
        {
            Options = new BitChartBarOptions()
            {
                Responsive = true,
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = "BitChart bar Chart"
                }
            }
        };

        var dataset = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            Label = "Dataset 1",
            BackgroundColor = BitChartDemoColors.All.Take(INITAL_COUNT).Select(c => BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, c))).ToArray()
        };
        _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
        _config.Data.Datasets.Add(dataset);
    }

    private void RandomizeBarData()
    {
        foreach (IDataset<int> dataset in _config.Data.Datasets)
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

        _chart.Update();
    }

    private void AddBarDataset()
    {
        System.Drawing.Color color = BitChartDemoColors.All[_config.Data.Datasets.Count % BitChartDemoColors.All.Count];
        IDataset<int> dataset = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(_config.Data.Labels.Count))
        {
            Label = $"Dataset {_config.Data.Datasets.Count + 1}",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, color)),
            BorderColor = BitChartColorUtil.FromDrawingColor(color),
            BorderWidth = 1
        };

        _config.Data.Datasets.Add(dataset);
        _chart.Update();
    }

    private void RemoveBarDataset()
    {
        IList<IBitChartDataset> datasets = _config.Data.Datasets;
        if (datasets.Count == 0)
            return;

        datasets.RemoveAt(datasets.Count - 1);
        _chart.Update();
    }

    private void AddBarData()
    {
        if (_config.Data.Datasets.Count == 0)
            return;

        string month = BitChartDemoUtils.Months[_config.Data.Labels.Count % BitChartDemoUtils.Months.Count];
        _config.Data.Labels.Add(month);

        foreach (IDataset<int> dataset in _config.Data.Datasets)
        {
            dataset.Add(BitChartDemoUtils.RandomScalingFactor());
        }

        _chart.Update();
    }

    private void RemoveBarData()
    {
        if (_config.Data.Datasets.Count == 0 ||
            _config.Data.Labels.Count == 0)
        {
            return;
        }

        _config.Data.Labels.RemoveAt(_config.Data.Labels.Count - 1);

        foreach (IDataset<int> dataset in _config.Data.Datasets)
        {
            dataset.RemoveAt(dataset.Count - 1);
        }

        _chart.Update();
    }



    private readonly string htmlCode = @"
<BitChart Config=""_config"" @ref=""_chart"" />

<div>
    <BitButton OnClick=""RandomizeBarData"">Randomize Data</BitButton>
    <BitButton OnClick= ""AddBarDataset"" > Add Dataset</BitButton>
    <BitButton OnClick= ""RemoveBarDataset"" > Remove Dataset</BitButton>
    <BitButton OnClick= ""AddBarData"" > Add Data</BitButton>
    <BitButton OnClick= ""RemoveBarData"" > Remove Data</BitButton>
</div>";
    private readonly string csharpCode = @"
private const int INITAL_COUNT = 5;

private BitChart _chart = default!;
private BitChartBarConfig _config = default!;

protected override void OnInitialized()
{
    _config = new BitChartBarConfig
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

    var dataset = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        Label = ""Dataset 1"",
        BackgroundColor = BitChartDemoColors.All.Take(INITAL_COUNT).Select(c => BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, c))).ToArray()
    };
    _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
    _config.Data.Datasets.Add(dataset);
}

private void RandomizeBarData()
{
    foreach (IDataset<int> dataset in _config.Data.Datasets)
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

    _chart.Update();
}

private void AddBarDataset()
{
    System.Drawing.Color color = BitChartDemoColors.All[_config.Data.Datasets.Count % BitChartDemoColors.All.Count];
    IDataset<int> dataset = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(_config.Data.Labels.Count))
    {
        Label = $""Dataset {_config.Data.Datasets.Count + 1}"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, color)),
        BorderColor = BitChartColorUtil.FromDrawingColor(color),
        BorderWidth = 1
    };

    _config.Data.Datasets.Add(dataset);
    _chart.Update();
}

private void RemoveBarDataset()
{
    IList<IBitChartDataset> datasets = _config.Data.Datasets;
    if (datasets.Count == 0)
        return;

    datasets.RemoveAt(datasets.Count - 1);
    _chart.Update();
}

private void AddBarData()
{
    if (_config.Data.Datasets.Count == 0)
        return;

    string month = BitChartDemoUtils.Months[_config.Data.Labels.Count % BitChartDemoUtils.Months.Count];
    _config.Data.Labels.Add(month);

    foreach (IDataset<int> dataset in _config.Data.Datasets)
    {
        dataset.Add(BitChartDemoUtils.RandomScalingFactor());
    }

    _chart.Update();
}

private void RemoveBarData()
{
    if (_config.Data.Datasets.Count == 0 ||
        _config.Data.Labels.Count == 0)
    {
        return;
    }

    _config.Data.Labels.RemoveAt(_config.Data.Labels.Count - 1);

    foreach (IDataset<int> dataset in _config.Data.Datasets)
    {
        dataset.RemoveAt(dataset.Count - 1);
    }

    _chart.Update();
}";
}
