using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Samples.Core.Pages;

public partial class ProgrammaticPage
{
    // Two shapes with the same commands - a move and four cubic curves - so every point has one to travel to.
    private const string BlobAPath = "M100 30 C140 30 170 60 170 100 C170 140 140 170 100 170 C60 170 30 140 30 100 C30 60 60 30 100 30";
    private const string BlobBPath = "M100 50 C130 20 180 70 150 100 C180 130 130 180 100 150 C70 180 20 130 50 100 C20 70 70 20 100 50";
    private static readonly BmProps BlobA = Bm.To(d: BlobAPath);
    private static readonly BmProps BlobB = Bm.To(d: BlobBPath);

    private BmAnimationControls? _controls;
    private CancellationTokenSource? _run;
    private bool _starting;
    private Task? _watched;
    private int _scrub;
    private string _seekLabel = "";

    private BmAnimationControls? _sequence;
    private CancellationTokenSource? _sequenceRun;

    // One value for every press, so a press while it is still counting replaces that animation instead of racing it.
    private readonly BmValue<double> _count = Bm.Value(0.0);
    private bool _counted;
    private bool _morphed;

    // Read from the controls on every render rather than kept up to date by hand: Pause, Play, a speed, Complete and
    // a seek all change it.
    private string RunState => _starting ? "starting" : _controls?.State.ToString().ToLowerInvariant() ?? "idle";

    private bool HasNoFrameLoop => Capabilities.IsPrerendering is false && Capabilities.SupportsFrameLoop is false;

    private string SequenceNote => HasNoFrameLoop
        ? "On this host there is no frame loop, so the gaps run on wall-clock timers instead and keep going while it is paused."
        : "";

    private string LoopNote => Capabilities.IsPrerendering
        ? ""
        : Capabilities.SupportsFrameLoop
            ? "This host has one: both animate."
            : "This host has none: both land at once, while every other animation on these pages still plays.";

    private async Task RunAsync()
    {
        // A press still starting is superseded: cancelling stops whatever its call hands back. Until this one's
        // controls arrive there is nothing to control.
        var token = Restart(ref _run);
        _controls?.Stop();
        _controls = null;
        _starting = true;
        _scrub = 0;
        _seekLabel = "";

        var controls = await Motion.AnimateAsync(
            ".prog-dot",
            Bm.To(y: [0, -48, 0], scale: [1, 1.3, 1]),
            Bm.Tween(2.4, BmEase.InOut),
            stagger: Bm.Stagger(0.12),
            cancellationToken: token);

        // A later press, or the page going away, has taken over; the controls are already stopped.
        if (token.IsCancellationRequested) return;

        _starting = false;
        _controls = controls;
        StateHasChanged();
        await WatchAsync(controls);
    }

    private Task PlayAsync()
    {
        if (_controls is null) return Task.CompletedTask;

        _controls.Play();
        return WatchAsync(_controls);
    }

    private Task SeekAsync(ChangeEventArgs e)
    {
        if (_controls is null || int.TryParse(e.Value?.ToString(), out var value) is false) return Task.CompletedTask;

        _scrub = value;
        if (_controls.State is BmPlaybackState.Running) _controls.Pause();
        _controls.Seek(value / 1000.0 * _controls.Duration);
        _seekLabel = $"{_controls.Time.ToString("0.00", CultureInfo.InvariantCulture)} s of {_controls.Duration.ToString("0.00", CultureInfo.InvariantCulture)} s";
        return WatchAsync(_controls);
    }

    // Waits for the animation to end, so the event that started the wait renders its final state. Play after it
    // finished and a seek after it ended take hold of it again, with a new end to wait for; one already waited on
    // is left to the handler waiting on it.
    private async Task WatchAsync(BmAnimationControls controls)
    {
        var done = controls.WhenCompleteAsync();
        if (ReferenceEquals(done, _watched)) return;

        _watched = done;
        await done;
    }

    private async Task PlaySequenceAsync()
    {
        var sequence = new BmSequence()
            .Add("#seq-box", Bm.To(x: 120), Bm.Tween(0.5, BmEase.InOut))
            .Add("#seq-box", Bm.To(rotate: 180), Bm.Tween(0.4), at: "-0.2")
            .Label("burst")
            .Add(".seq-dot", Bm.To(scale: [1, 1.8, 1]), Bm.Spring(stiffness: 400, damping: 16), at: "burst")
            .Add("#seq-box", Bm.To(x: 0, rotate: 0), Bm.Spring(stiffness: 200, damping: 18), at: "+0.2");

        // Stop what a previous press left playing, or is still starting, so the two runs don't fight over the box.
        var token = Restart(ref _sequenceRun);
        _sequence?.Stop();
        _sequence = null;

        var controls = await Motion.RunAsync(sequence, token);
        if (token.IsCancellationRequested) return;

        _sequence = controls;
    }

    private Task CountAsync()
    {
        _counted = _counted is false;

        return Motion.AnimateAsync(_count, _counted ? 2_450 : 0, Bm.Tween(1.4, BmEase.InOut));
    }

    // Cancels what the last press started and hands out the token for the next one.
    private static CancellationToken Restart(ref CancellationTokenSource? source)
    {
        source?.Cancel();
        source?.Dispose();
        source = new CancellationTokenSource();
        return source.Token;
    }

    public void Dispose()
    {
        // Cancelling stops what a press still starting hands back once it arrives.
        _run?.Cancel();
        _sequenceRun?.Cancel();
        _controls?.Stop();
        _sequence?.Stop();
        _count.Dispose();
    }
}
