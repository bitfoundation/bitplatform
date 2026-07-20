namespace Bit.BlazorUI;

/// <summary>Largest-Triangle-Three-Buckets downsampling for line series.</summary>
public static class BitChartDecimation
{
    public static List<(double x, double y, int di, double v)> Lttb(
        List<(double x, double y, int di, double v)> data, int threshold)
    {
        int n = data.Count;
        if (threshold >= n || threshold < 3) return data;

        var sampled = new List<(double x, double y, int di, double v)>(threshold) { data[0] };
        double bucketSize = (double)(n - 2) / (threshold - 2);
        int a = 0;

        for (int i = 0; i < threshold - 2; i++)
        {
            int rangeStart = (int)Math.Floor((i + 1) * bucketSize) + 1;
            int rangeEnd = (int)Math.Floor((i + 2) * bucketSize) + 1;
            rangeEnd = Math.Min(rangeEnd, n);

            // Average point of the next bucket.
            double avgX = 0, avgY = 0;
            int count = rangeEnd - rangeStart;
            if (count <= 0) { count = 1; rangeEnd = Math.Min(rangeStart + 1, n); }
            for (int j = rangeStart; j < rangeEnd; j++) { avgX += data[j].x; avgY += data[j].y; }
            avgX /= count; avgY /= count;

            // Current bucket range.
            int curStart = (int)Math.Floor(i * bucketSize) + 1;
            int curEnd = (int)Math.Floor((i + 1) * bucketSize) + 1;

            double pax = data[a].x, pay = data[a].y;
            double maxArea = -1;
            int next = curStart;
            for (int j = curStart; j < curEnd && j < n; j++)
            {
                double area = Math.Abs((pax - avgX) * (data[j].y - pay) - (pax - data[j].x) * (avgY - pay)) * 0.5;
                if (area > maxArea) { maxArea = area; next = j; }
            }
            sampled.Add(data[next]);
            a = next;
        }

        sampled.Add(data[n - 1]);
        return sampled;
    }
}
