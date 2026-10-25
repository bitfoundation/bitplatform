using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Bit.Bmotion;
/// <summary>
/// Controls for an in-flight programmatic animation started by
/// <see cref="BmotionAnimateService"/>.
/// <para>The object is directly awaitable - <c>await controls</c> waits for the animation to complete.</para>
/// <para>
/// <see cref="Stop"/> freezes the animation at its current (intermediate) values; <see cref="Complete"/>
/// jumps it to its target (end) values. Both release the engine refcount immediately, so they also
/// safely stop infinite-repeat animations (whose completion task never resolves on its own).
/// </para>
/// <para>
/// <see cref="Time"/>, <see cref="Duration"/>, <see cref="Progress"/> and <see cref="State"/> report where the
/// animation is (motion.dev's <c>time</c>/<c>duration</c>/<c>state</c>); <see cref="Seek"/> (or setting
/// <see cref="Time"/>) moves it - in place for animations the browser plays, which makes scrubbing with a slider
/// cheap, and otherwise by playing it again from that point.
/// </para>
/// </summary>
public sealed class BmAnimationControls
{
    private readonly IReadOnlyList<string> _elementIds;
    private readonly BmotionAnimationEngine _engine;
    private readonly Action _release;
    // Set for a BmSequence: its segment starts live on a timeline playhead, not on the elements,
    // so the rate has to reach the playhead too or pausing/speeding a timeline would only affect
    // whichever segment happened to be running.
    private readonly Action<double>? _setTimelineRate;
    private readonly BmotionPlayback? _playback;
    private TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _released;
    // Serializes the release/claim decision with the engine calls it guards: without it,
    // SetSpeed could observe _released == 0, lose the CPU, and then touch elements a concurrent
    // Stop/Complete/natural settlement has already released (and newer animations now own).
    private readonly object _sync = new();
    // The rate each element played at before SetSpeed first changed it. The rate lives on the
    // elements themselves, so it has to be put back when these controls let go of them - otherwise
    // a <Bmotion> owning one of them would find every later animation of its own frozen or re-timed.
    // Put back to what it was, not to realtime: an element someone else had paused stays paused.
    private Dictionary<string, double>? _ratesBefore;

    // The playhead: the animation time played before the last change of speed or seek, since when, and at what speed.
    private double _playedBefore;
    private long _since = Stopwatch.GetTimestamp();
    private double _speed = 1;
    private BmPlaybackState _state = BmPlaybackState.Running;
    // A seek that can't move the animation in place plays it again from there: a new run, starting that far in. Only
    // the latest run's end is the animation's.
    private int _run;
    private double _runOffset;

    internal BmAnimationControls(
        IReadOnlyList<string> elementIds, BmotionAnimationEngine engine, Task completion, Action release,
        Action<double>? setTimelineRate = null, BmotionPlayback? playback = null)
    {
        _elementIds = elementIds;
        _engine = engine;
        _release = release;
        _setTimelineRate = setTimelineRate;
        _playback = playback;
        Watch(completion, _run);
    }

    /// <summary>Controls for an animation cancelled before it started: stopped at its start, nothing to wait for.</summary>
    internal static BmAnimationControls Stopped(BmotionAnimationEngine engine)
    {
        var controls = new BmAnimationControls(Array.Empty<string>(), engine, new TaskCompletionSource().Task, static () => { });
        controls._state = BmPlaybackState.Stopped;
        controls._released = 1;
        controls._done.TrySetResult();
        return controls;
    }

    // ── Where it is ───────────────────────────────────────────────────────────

    /// <summary>
    /// How long the animation lasts in seconds - delays, staggers and repeats included, a spring's settling time
    /// estimated; <see cref="double.PositiveInfinity"/> when it repeats forever.
    /// </summary>
    public double Duration => _playback?.Duration ?? 0;

    /// <summary>
    /// How far into the animation the playhead is, in seconds of animation time (a speed of 2 advances it twice as
    /// fast). Setting it seeks (see <see cref="Seek"/>).
    /// </summary>
    public double Time
    {
        get { lock (_sync) return CurrentTime(); }
        set => Seek(value);
    }

    /// <summary>
    /// <see cref="Time"/> as a fraction of <see cref="Duration"/>, 0 to 1; 0 for an animation without a finite length.
    /// </summary>
    public double Progress
    {
        get
        {
            double duration = Duration;
            return double.IsFinite(duration) && duration > 0 ? Math.Clamp(Time / duration, 0, 1) : 0;
        }
    }

    /// <summary>Whether it is playing, paused, finished or stopped.</summary>
    public BmPlaybackState State
    {
        get { lock (_sync) return _state; }
    }

    private double CurrentTime()
    {
        if (_state is BmPlaybackState.Finished or BmPlaybackState.Stopped) return _playedBefore;
        double time = _playedBefore + (Stopwatch.GetElapsedTime(_since).TotalSeconds * _speed);
        return double.IsFinite(Duration) ? Math.Min(time, Duration) : time;
    }

    // ── Playback ──────────────────────────────────────────────────────────────

    /// <summary>Pauses the animation in place (equivalent to <c>Speed = 0</c>).</summary>
    public void Pause() => SetSpeed(0);

    /// <summary>
    /// Resumes a paused animation at normal speed. A finished animation plays again from the start, and a stopped
    /// one plays on from where it stopped.
    /// </summary>
    public void Play()
    {
        BmPlaybackState state;
        lock (_sync) state = _state;
        // Taken hold of again already playing, so the replay starts the way the first run did.
        if (state == BmPlaybackState.Finished) SeekCore(0, hold: false);
        else if (state == BmPlaybackState.Stopped) SeekCore(Time, hold: false);
        else SetSpeed(1);
    }

    /// <summary>
    /// Sets the playback rate: 1 = realtime, 0 = paused, 2 = twice as fast.
    /// No-op once the animation has settled (stopped, completed, or finished naturally) -
    /// the elements may already be owned by newer animations.
    /// </summary>
    public void SetSpeed(double speed)
    {
        lock (_sync)
        {
            if (System.Threading.Volatile.Read(ref _released) != 0) return;
            speed = double.IsFinite(speed) && speed >= 0 ? speed : 0;
            _playedBefore = CurrentTime();
            _since = Stopwatch.GetTimestamp();
            _speed = speed;
            _state = speed == 0 ? BmPlaybackState.Paused : BmPlaybackState.Running;
            ApplySpeed(speed);
        }
    }

    // The speed reaches the elements (and, for a sequence, its timeline playhead).
    private void ApplySpeed(double speed)
    {
        if (_ratesBefore is null)
        {
            _ratesBefore = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var id in _elementIds)
                _ratesBefore.TryAdd(id, _engine.GetPlaybackRate(id));
        }
        foreach (var id in _elementIds)
            _engine.SetPlaybackRate(id, speed);
        // Segments that haven't started yet are governed by the timeline playhead, so the same
        // rate applies there - otherwise a paused sequence would keep starting its later steps.
        _setTimelineRate?.Invoke(speed);
    }

    /// <summary>
    /// Moves the animation to <paramref name="seconds"/> into it (clamped to 0 and <see cref="Duration"/>), keeping its
    /// speed: a playing animation plays on from there, a paused one holds there. What the browser plays moves in place,
    /// exactly and without restarting - cheap enough to scrub with; anything else is played again from that point,
    /// from the values the animation started from. A finished or stopped animation is taken hold of again and holds
    /// at that point (paused) until <see cref="Play"/>.
    /// </summary>
    /// <remarks>
    /// A sequence is always played again from the point: its segments that would be over by then land on their end
    /// values, so two segments that overlap on one property start the later from the earlier's end value.
    /// </remarks>
    public void Seek(double seconds)
    {
        if (!double.IsFinite(seconds))
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A seek needs a finite time in seconds.");
        SeekCore(seconds, hold: true);
    }

    // hold: an ended animation taken hold of again holds at the point (a seek) rather than playing on (Play).
    private void SeekCore(double seconds, bool hold)
    {
        if (_playback is not { } playback) return;
        seconds = Math.Max(0, seconds);
        if (double.IsFinite(playback.Duration)) seconds = Math.Min(seconds, playback.Duration);

        lock (_sync)
        {
            bool ended = _state is BmPlaybackState.Finished or BmPlaybackState.Stopped;
            if (!ended && seconds >= _runOffset && playback.TrySeekInPlace(seconds - _runOffset))
            {
                _playedBefore = seconds;
                _since = Stopwatch.GetTimestamp();
                return;
            }

            if (ended)
            {
                // Taken hold of again: held at the new point until played, or playing on from it.
                playback.Rearm();
                System.Threading.Volatile.Write(ref _released, 0);
                _done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _ratesBefore = null;
                _speed = hold ? 0 : 1;
                _state = hold ? BmPlaybackState.Paused : BmPlaybackState.Running;
                if (hold) ApplySpeed(0);
            }

            _run++;
            _runOffset = seconds;
            _playedBefore = seconds;
            _since = Stopwatch.GetTimestamp();
            Watch(playback.PlayFrom(seconds), _run);
        }
    }

    /// <summary>
    /// Immediately cancel all running animations on the target elements.
    /// Elements snap to their current (intermediate) positions.
    /// </summary>
    public void Stop()
    {
        // Atomically claim ownership: only the caller that flips _released from 0→1 runs the engine
        // side effects. Once released (e.g. a natural finish already settled the completion), the
        // target elements may be owned by newer animations - skip so we don't disturb them.
        lock (_sync)
        {
            if (System.Threading.Interlocked.CompareExchange(ref _released, 1, 0) != 0) return;
            _playedBefore = CurrentTime();
            _state = BmPlaybackState.Stopped;
            try
            {
                foreach (var id in _elementIds)
                    _engine.Stop(id, null);
                RestoreRate();
            }
            finally
            {
                _release();
            }
        }
    }

    /// <summary>
    /// Cancel all running animations and snap elements to their target (end) values.
    /// </summary>
    public void Complete()
    {
        // See Stop(): atomically claim ownership so engine side effects run exactly once and never
        // after a concurrent settlement has handed the elements to newer animations.
        lock (_sync)
        {
            if (System.Threading.Interlocked.CompareExchange(ref _released, 1, 0) != 0) return;
            _playedBefore = double.IsFinite(Duration) ? Duration : CurrentTime();
            _state = BmPlaybackState.Finished;
            try
            {
                foreach (var id in _elementIds)
                    _engine.Complete(id);
                RestoreRate();
            }
            finally
            {
                _release();
            }
        }
    }

    /// <summary>A <see cref="Task"/> that resolves when the animation finishes, is stopped or is completed.</summary>
    public Task WhenCompleteAsync()
    {
        lock (_sync) return _done.Task;
    }

    /// <summary>Makes <see cref="BmAnimationControls"/> directly awaitable.</summary>
    public TaskAwaiter GetAwaiter() => WhenCompleteAsync().GetAwaiter();

    // ── Settling ──────────────────────────────────────────────────────────────

    private void Watch(Task run, int generation) => run.OnCallerContext(t => OnRunSettled(t, generation));

    private void OnRunSettled(Task run, int generation)
    {
        TaskCompletionSource done;
        lock (_sync)
        {
            // A seek played the animation again: the run it replaced ending isn't the animation ending.
            if (generation != _run) return;
            if (_state is BmPlaybackState.Running or BmPlaybackState.Paused)
            {
                _playedBefore = double.IsFinite(Duration) ? Duration : CurrentTime();
                _state = BmPlaybackState.Finished;
            }
            done = _done;
        }
        // Release the engine refcount exactly once - whether the animation finishes naturally, is
        // stopped, or is completed early. Without this, an infinite-repeat animation (which never
        // finishes) would pin its elements in the engine forever.
        ReleaseOnce();
        if (run.IsFaulted) done.TrySetException(run.Exception!.InnerExceptions);
        else done.TrySetResult();
    }

    private void ReleaseOnce()
    {
        lock (_sync)
        {
            if (System.Threading.Interlocked.Exchange(ref _released, 1) == 0)
            {
                RestoreRate();
                _release();
            }
        }
    }

    private void RestoreRate()
    {
        if (_ratesBefore is not { } rates) return;
        _ratesBefore = null;
        foreach (var (id, rate) in rates)
            _engine.SetPlaybackRate(id, rate);
    }
}

/// <summary>
/// What a programmatic animation's controls need to report and move its time: how long it lasts, how to move it in
/// place, how to play it again from part-way in, and how to take hold of its elements again once it has ended.
/// </summary>
internal sealed class BmotionPlayback
{
    /// <summary>The animation's length in seconds (see <see cref="BmAnimationControls.Duration"/>).</summary>
    public double Duration { get; init; }

    /// <summary>Moves the current run to this many seconds into it in place; false when it can't.</summary>
    public Func<double, bool> TrySeekInPlace { get; init; } = static _ => false;

    /// <summary>Plays the animation again from this many seconds in; returns the new run's completion.</summary>
    public required Func<double, Task> PlayFrom { get; init; }

    /// <summary>Registers the elements with the engine again, after the animation released them.</summary>
    public required Action Rearm { get; init; }
}
