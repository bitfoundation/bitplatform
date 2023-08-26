namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Chart;

public partial class _BitChartPieDemo
{
    private const int INITAL_COUNT = 5;

    private BitChart _chart = default!;
    private BitChartPieConfig _config = default!;

    protected override void OnInitialized()
    {
        _config = new BitChartPieConfig
        {
            Options = new BitChartPieOptions
            {
                Responsive = true,
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = "BitChart Pie Chart"
                }
            }
        };

        BitChartPieDataset<int> dataset = new BitChartPieDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            BackgroundColor = BitChartDemoColors.All.Take(INITAL_COUNT).Select(c => BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(220, c))).ToArray()
        };
        _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
        _config.Data.Datasets.Add(dataset);
    }

    private void RandomizePieData()
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

    private void AddPieDataset()
    {
        int count = _config.Data.Labels.Count;
        BitChartPieDataset<int> dataset = new BitChartPieDataset<int>(BitChartDemoUtils.RandomScalingFactor(count, -100, 100))
        {
            BackgroundColor = BitChartDemoColors.All.Take(count).Select(BitChartColorUtil.FromDrawingColor).ToArray()
        };

        _config.Data.Datasets.Add(dataset);
        _chart.Update();
    }

    private void RemovePieDataset()
    {
        IList<IBitChartDataset> datasets = _config.Data.Datasets;
        if (datasets.Count == 0)
            return;

        datasets.RemoveAt(0);
        _chart.Update();
    }

    private void AddPieData()
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

    private void RemovePieData()
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
    <BitButton OnClick=""RandomizePieData"">Randomize Data</BitButton>
    <BitButton OnClick=""AddPieDataset"">Add Dataset</BitButton>
    <BitButton OnClick=""RemovePieDataset"">Remove Dataset</BitButton>
    <BitButton OnClick=""AddPieData"">Add Data</BitButton>
    <BitButton OnClick=""RemovePieData"">Remove Data</BitButton>
</div>";
    private readonly string csharpCode = @"
private const int INITAL_COUNT = 5;

private BitChart _chart = default!;
private BitChartPieConfig _config = default!;

protected override void OnInitialized()
{
    _config = new BitChartPieConfig
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

    BitChartPieDataset<int> dataset = new BitChartPieDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        BackgroundColor = BitChartDemoColors.All.Take(INITAL_COUNT).Select(c => BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(220, c))).ToArray()
    };
    _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
    _config.Data.Datasets.Add(dataset);
}

private void RandomizePieData()
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

private void AddPieDataset()
{
    int count = _config.Data.Labels.Count;
    BitChartPieDataset<int> dataset = new BitChartPieDataset<int>(BitChartDemoUtils.RandomScalingFactor(count, -100, 100))
    {
        BackgroundColor = BitChartDemoColors.All.Take(count).Select(BitChartColorUtil.FromDrawingColor).ToArray()
    };

    _config.Data.Datasets.Add(dataset);
    _chart.Update();
}

private void RemovePieDataset()
{
    IList<IBitChartDataset> datasets = _config.Data.Datasets;
    if (datasets.Count == 0)
        return;

    datasets.RemoveAt(0);
    _chart.Update();
}

private void AddPieData()
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

private void RemovePieData()
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
