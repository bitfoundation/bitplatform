using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>A tool context for tests: a real document, history and selection with no browser involved.</summary>
internal sealed class TestToolContext : IBinkToolContext, IDisposable
{
    public TestToolContext(BinkDocument? document = null)
    {
        Document = document ?? new BinkDocument(new BinkDocumentSettings { Width = 800, Height = 600 });
        History = new BinkHistoryManager(Document);
        Selection = new BinkSelection(Document);
        ActiveLayerId = Document.Layers[0].Id;
    }

    public BinkDocument Document { get; }

    public BinkSelection Selection { get; }

    public BinkHistoryManager History { get; }

    public BinkViewport Viewport { get; set; } = BinkViewport.Identity;

    public Guid ActiveLayerId { get; set; }

    public BinkBrush Brush { get; set; } = new BinkPenBrush();

    public BinkShapeStyle ShapeStyle { get; set; } = BinkShapeStyle.Default;

    public Vector2 ViewSize { get; set; } = new(800, 600);

    public int OverlayInvalidations { get; private set; }

    public int DocumentInvalidations { get; private set; }

    public List<string> Announcements { get; } = new();

    public string Cursor { get; private set; } = "default";

    public HashSet<int> CapturedPointers { get; } = new();

    public void InvalidateOverlay() => OverlayInvalidations++;

    public void InvalidateDocument(BinkRect? region = null) => DocumentInvalidations++;

    public void CapturePointer(int pointerId) => CapturedPointers.Add(pointerId);

    public void ReleasePointer(int pointerId) => CapturedPointers.Remove(pointerId);

    public void SetCursor(string cursor) => Cursor = cursor;

    public void Announce(string message) => Announcements.Add(message);

    /// <summary>Runs a complete gesture through a tool: down, a series of moves, then up.</summary>
    public void Gesture(IBinkTool tool, BinkPointerKind kind, params (float X, float Y, float Pressure)[] points)
    {
        ArgumentNullException.ThrowIfNull(tool);
        double time = 0;
        BinkInputSample Make(int index, BinkPointerPhase phase) => new()
        {
            PointerId = 1,
            Kind = kind,
            Phase = phase,
            X = points[index].X,
            Y = points[index].Y,
            Pressure = points[index].Pressure,
            Timestamp = time,
            Buttons = phase == BinkPointerPhase.Up ? 0 : 1,
            IsPrimary = true,
        };

        tool.OnPointerDown(this, Make(0, BinkPointerPhase.Down));
        for (int i = 1; i < points.Length - 1; i++)
        {
            time += 8;
            tool.OnPointerMove(this, Make(i, BinkPointerPhase.Move));
        }
        time += 8;
        tool.OnPointerUp(this, Make(points.Length - 1, BinkPointerPhase.Up));
    }

    /// <summary>Runs a straight-line gesture with a given number of samples.</summary>
    public void Line(IBinkTool tool, Vector2 from, Vector2 to, int samples = 20, BinkPointerKind kind = BinkPointerKind.Mouse, float pressure = 0.5f)
    {
        var points = new (float, float, float)[samples];
        for (int i = 0; i < samples; i++)
        {
            Vector2 p = Vector2.Lerp(from, to, samples == 1 ? 0 : i / (float)(samples - 1));
            points[i] = (p.X, p.Y, pressure);
        }
        Gesture(tool, kind, points);
    }

    public void Dispose()
    {
        History.Dispose();
        Selection.Dispose();
    }
}
