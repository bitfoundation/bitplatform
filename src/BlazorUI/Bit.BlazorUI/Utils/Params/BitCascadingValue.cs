using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.ExceptionServices;

namespace Bit.BlazorUI;

/// <summary>
/// The cascading value to be provided using the <see cref="BitCascadingValueProvider"/> component.
/// </summary>
public class BitCascadingValue
{
    private readonly object _lock = new();

    // Held around the whole read-modify-apply of the AutoNotify subscription, so that two threads - a
    // background mutation and the renderer thread subscribing - cannot interleave and leave the observed
    // value pointing at an object no handler is attached to, or a handler attached to an untracked object.
    private readonly object _observationLock = new();

    private object? _value;
    private string? _name;
    private bool _isFixed;
    private bool _enabled;
    private bool _autoNotify;
    private bool _computedRead;
    private object? _observedValue;
    private Func<object?>? _valueFactory;
    private Func<object?>? _computedFactory;

    private Action<BitCascadingValue>? _changed;
    private Func<BitCascadingValue, Task>? _changedAsync;



    /// <summary>
    /// Creates a new cascading value.
    /// </summary>
    /// <param name="value">The value to be provided.</param>
    /// <param name="name">The optional name of the cascading value.</param>
    /// <param name="isFixed">Determines that the value will not change.</param>
    /// <param name="valueType">
    /// The type to be used as the TValue of the underlying CascadingValue component.
    /// When not provided, the runtime type of the <paramref name="value"/> is used, so it must be
    /// provided whenever the value is null or its static type differs from its runtime type.
    /// </param>
    /// <param name="enabled">Determines that the value is provided at all.</param>
    public BitCascadingValue(object? value, string? name, bool isFixed, Type? valueType = null, bool enabled = true)
    {
        ValueType = ValidateValueType(valueType
                 ?? value?.GetType()
                 ?? throw new ArgumentNullException(nameof(valueType), "Either the value must be non-null or the valueType must be explicitly provided."));

        ValidateValue(value, ValueType);

        _value = value;
        _name = NormalizeName(name);
        _isFixed = isFixed;
        _enabled = enabled;
    }

    public BitCascadingValue(object? value, string? name = null) : this(value, name, false) { }
    public BitCascadingValue(object? value, bool isFixed) : this(value, null, isFixed) { }
    public BitCascadingValue(object? value, Type valueType) : this(value, null, false, valueType) { }
    public BitCascadingValue(object? value, string name, Type valueType) : this(value, name, false, valueType) { }

    private BitCascadingValue(Func<object?> valueFactory, bool isComputed, Type valueType, string? name, bool isFixed, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);
        ArgumentNullException.ThrowIfNull(valueType);

        ValueType = ValidateValueType(valueType);

        if (isComputed)
        {
            _computedFactory = valueFactory;
        }
        else
        {
            _valueFactory = valueFactory;
        }

        _name = NormalizeName(name);
        _isFixed = isFixed;
        _enabled = enabled;
    }



    /// <summary>
    /// Raised whenever this cascading value changes, which is what lets the hosting
    /// <see cref="BitCascadingValueProvider"/> re-render itself and push the current values down to the
    /// consumers without the component that owns the values having to re-render.
    /// Assigning <see cref="Value"/>, <see cref="Name"/>, <see cref="IsFixed"/> or <see cref="Enabled"/>
    /// raises it automatically; <see cref="NotifyChanged"/> raises it on demand.
    /// </summary>
    public event Action<BitCascadingValue>? Changed
    {
        add
        {
            lock (_lock)
            {
                _changed += value;
            }

            UpdateObservation();
        }
        remove
        {
            lock (_lock)
            {
                _changed -= value;
            }

            UpdateObservation();
        }
    }

    /// <summary>
    /// The awaitable counterpart of <see cref="Changed"/>, which is what makes
    /// <see cref="NotifyChangedAsync"/> complete only once every listening
    /// <see cref="BitCascadingValueProvider"/> has re-rendered and pushed the value down to the consumers.
    /// </summary>
    public event Func<BitCascadingValue, Task>? ChangedAsync
    {
        add
        {
            lock (_lock)
            {
                _changedAsync += value;
            }

            UpdateObservation();
        }
        remove
        {
            lock (_lock)
            {
                _changedAsync -= value;
            }

            UpdateObservation();
        }
    }



    /// <summary>
    /// The value to be provided. Assigning a value that is not assignable to the <see cref="ValueType"/>
    /// throws an <see cref="ArgumentException"/>.
    /// When the value comes from a lazy factory, the factory runs the first time this property is read;
    /// when it comes from a computed factory, the factory runs on every read.
    /// </summary>
    public object? Value
    {
        get
        {
            object? created;

            // Created under the lock so that the lazy factory still runs exactly once and its value is
            // published to every thread that reads it, which is what Lazy<T> does by default as well.
            lock (_lock)
            {
                var computedFactory = _computedFactory;

                if (computedFactory is null)
                {
                    var valueFactory = _valueFactory;

                    if (valueFactory is null) return _value;

                    // Dropped before it runs, so a factory that ends up reading this very value reads what
                    // is stored rather than running itself again, and put back whenever it fails, so that a
                    // factory that throws is retried on the next read rather than pinning the value at the
                    // null a non-nullable value type could not even be cascaded as.
                    _valueFactory = null;

                    try
                    {
                        created = valueFactory();

                        ValidateValue(created, ValueType);
                    }
                    catch
                    {
                        _valueFactory = valueFactory;

                        throw;
                    }
                }
                else
                {
                    created = computedFactory();

                    ValidateValue(created, ValueType);

                    _computedRead = true;
                }

                _value = created;
            }

            UpdateObservation();

            return created;
        }
        set
        {
            ValidateValue(value, ValueType);

            bool changed;

            lock (_lock)
            {
                changed = _valueFactory is not null || _computedFactory is not null || Equals(_value, value) is false;

                _valueFactory = null;
                _computedFactory = null;
                _computedRead = false;
                _value = value;
            }

            UpdateObservation();

            if (changed)
            {
                NotifyChanged();
            }
        }
    }

    /// <summary>
    /// The optional name of the cascading value. An empty or white-space name is treated as no name at all.
    /// The consumers match it case-insensitively, exactly like the Name of a CascadingValue component does.
    /// </summary>
    /// <remarks>
    /// The framework resolves which supplier feeds a cascading parameter once, when the consuming component
    /// is created, so the <see cref="BitCascadingValueProvider"/> re-creates the underlying CascadingValue
    /// component - and with it the content below it - whenever a name changes, which is what lets the
    /// consumers be matched again under the new name.
    /// </remarks>
    public string? Name
    {
        get => _name;
        set
        {
            var name = NormalizeName(value);

            if (string.Equals(_name, name, StringComparison.Ordinal)) return;

            _name = name;

            NotifyChanged();
        }
    }

    /// <summary>
    /// If true, indicates that <see cref="Value"/> will not change, so the consumers are never subscribed
    /// for change notifications, which is the cheapest way of cascading a value that is created once.
    /// Toggling it re-creates the underlying CascadingValue component, because the framework does not let
    /// the IsFixed of a live CascadingValue change.
    /// </summary>
    public bool IsFixed
    {
        get => _isFixed;
        set
        {
            if (_isFixed == value) return;

            _isFixed = value;

            NotifyChanged();
        }
    }

    /// <summary>
    /// Determines whether this cascading value is provided to the children. A disabled value is skipped
    /// by the <see cref="BitCascadingValueProvider"/> as if it was never added, which lets an outer or a
    /// root level cascading value of the same type or name show through.
    /// Toggling it changes the shape of the rendered tree, so the child content is re-created just like
    /// it would be when a CascadingValue component is wrapped in a conditional block.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;

            _enabled = value;

            NotifyChanged();
        }
    }

    /// <summary>
    /// Watches the cascaded value itself and raises <see cref="Changed"/> whenever it reports that it was
    /// mutated in place, which is the automatic counterpart of calling <see cref="NotifyChanged"/> by hand.
    /// A value implementing <see cref="INotifyCollectionChanged"/> is watched for its collection changes,
    /// one implementing only <see cref="INotifyPropertyChanged"/> for its property changes, and any other
    /// value is left alone. The subscription is only held while at least one listener - a hosting
    /// <see cref="BitCascadingValueProvider"/>, typically - is attached, so a value that outlives the
    /// provider never keeps this cascading value alive.
    /// </summary>
    public bool AutoNotify
    {
        get => _autoNotify;
        set
        {
            if (_autoNotify == value) return;

            _autoNotify = value;

            UpdateObservation();
        }
    }

    /// <summary>
    /// The actual type of the value to be used as the TValue of the CascadingValue component.
    /// </summary>
    public Type ValueType { get; }

    /// <summary>
    /// Whether <see cref="Value"/> is already available. It is only false for a value created from a lazy
    /// factory that has not run successfully yet, which is the case until the value is provided for the
    /// first time, so a disabled or a shadowed value never gets there. A computed value is produced on
    /// every read, so it always reports true.
    /// </summary>
    public bool IsValueCreated => _valueFactory is null;

    /// <summary>
    /// Whether the value is produced by a factory that runs on every read rather than being stored once,
    /// which is what the Computed factory methods create.
    /// </summary>
    public bool IsComputed => _computedFactory is not null;



    /// <summary>
    /// Raises the <see cref="Changed"/> and the <see cref="ChangedAsync"/> events so that the hosting
    /// <see cref="BitCascadingValueProvider"/> re-renders and pushes this value down to the consumers again.
    /// Assigning any of the properties does it already, so this is the escape hatch for a cascaded object
    /// that is mutated in place. Use <see cref="NotifyChangedAsync"/> to await the resulting re-render.
    /// </summary>
    public void NotifyChanged()
    {
        var task = NotifyChangedAsync();

        if (task.IsCompletedSuccessfully) return;

        // Observed so that a re-render that fails on a background thread does not surface as an unobserved
        // task exception; the renderer already reports it to the component that threw.
        _ = task.ContinueWith(static t => _ = t.Exception,
                              CancellationToken.None,
                              TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                              TaskScheduler.Default);
    }

    /// <summary>
    /// Raises the <see cref="Changed"/> and the <see cref="ChangedAsync"/> events and returns a task that
    /// completes once every listening <see cref="BitCascadingValueProvider"/> has re-rendered, which is the
    /// counterpart of the NotifyChangedAsync method of the framework's CascadingValueSource.
    /// Every handler is invoked even when another one fails, and the failures are reported through the
    /// returned task - a single one as it was thrown, several as an <see cref="AggregateException"/>.
    /// </summary>
    public Task NotifyChangedAsync()
    {
        Action<BitCascadingValue>? changed;
        Func<BitCascadingValue, Task>? changedAsync;

        lock (_lock)
        {
            changed = _changed;
            changedAsync = _changedAsync;
        }

        // Every handler is invoked even when an earlier one fails, so that a provider throwing while it
        // re-renders cannot stop the other providers sharing this value from being refreshed. The failures
        // are collected and surfaced through the returned task rather than out of the property assignment
        // that raised the event.
        List<Exception>? errors = null;
        List<Task>? tasks = null;

        if (changed is not null)
        {
            var handlers = changed.GetInvocationList();

            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    ((Action<BitCascadingValue>)handlers[i])(this);
                }
                catch (Exception ex)
                {
                    (errors ??= []).Add(ex);
                }
            }
        }

        if (changedAsync is not null)
        {
            var handlers = changedAsync.GetInvocationList();

            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    var task = ((Func<BitCascadingValue, Task>)handlers[i])(this);

                    if (task is not null && task.IsCompletedSuccessfully is false)
                    {
                        (tasks ??= []).Add(task);
                    }
                }
                catch (Exception ex)
                {
                    (errors ??= []).Add(ex);
                }
            }
        }

        if (errors is null)
        {
            if (tasks is null) return Task.CompletedTask;

            return tasks.Count == 1 ? tasks[0] : Task.WhenAll(tasks);
        }

        if (tasks is null)
        {
            return Task.FromException(errors.Count == 1 ? errors[0] : new AggregateException(errors));
        }

        return AwaitHandlersAsync(tasks, errors);
    }

    /// <summary>
    /// Awaits the handlers that did not complete synchronously and reports their failures together with the
    /// ones that already threw, so that a single failing listener neither hides the others nor is dropped.
    /// </summary>
    private static async Task AwaitHandlersAsync(List<Task> tasks, List<Exception> errors)
    {
        foreach (var task in tasks)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }

        if (errors.Count == 1)
        {
            ExceptionDispatchInfo.Capture(errors[0]).Throw();
        }

        throw new AggregateException(errors);
    }



    /// <summary>
    /// Creates a cascading value whose ValueType is the static type of <typeparamref name="T"/>, which is
    /// the safe way of cascading null values, nullable value types, interfaces and base types.
    /// </summary>
    public static BitCascadingValue From<T>(T value, string? name = null, bool isFixed = false, bool enabled = true)
        => new(value, name, isFixed, typeof(T), enabled);

    /// <summary>
    /// Creates a cascading value whose ValueType is the static type of <typeparamref name="T"/>.
    /// </summary>
    public static BitCascadingValue From<T>(T value, bool isFixed, bool enabled = true)
        => new(value, null, isFixed, typeof(T), enabled);

    /// <summary>
    /// Creates a fixed (IsFixed) cascading value whose ValueType is the static type of <typeparamref name="T"/>.
    /// Fixed values never subscribe their consumers for change notifications, so they are the cheapest way
    /// of cascading a value that never changes.
    /// </summary>
    public static BitCascadingValue Fixed<T>(T value, string? name = null, bool enabled = true)
        => new(value, name, true, typeof(T), enabled);

    /// <summary>
    /// Creates a cascading value whose ValueType is the static type of <typeparamref name="T"/> and whose
    /// value is produced by <paramref name="valueFactory"/> the first time it is actually needed, so an
    /// expensive value is never built for a disabled entry, for an entry that a later one shadows, or for
    /// a provider that is never rendered. The factory runs at most once, unless it throws, in which case
    /// the exception is surfaced to the reader and the factory is run again on the next read.
    /// </summary>
    public static BitCascadingValue Lazy<T>(Func<T> valueFactory, string? name = null, bool isFixed = false, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        return new(() => valueFactory(), false, typeof(T), name, isFixed, enabled);
    }

    /// <summary>
    /// Creates a lazily produced cascading value with an explicit ValueType, which is the way of deferring
    /// the creation of a value whose cascaded type is only known at runtime. The factory runs at most once,
    /// unless it throws, in which case it is run again on the next read.
    /// </summary>
    public static BitCascadingValue Lazy(Func<object?> valueFactory, Type valueType, string? name = null, bool isFixed = false, bool enabled = true)
        => new(valueFactory, false, valueType, name, isFixed, enabled);

    /// <summary>
    /// Creates a cascading value that is re-read from <paramref name="valueFactory"/> every time it is
    /// provided, so one long lived BitCascadingValue keeps tracking the state it is derived from without
    /// the collection of values having to be rebuilt on every render. The provider reads it once per
    /// render, and <see cref="NotifyChanged"/> pushes a fresh reading down on demand.
    /// </summary>
    public static BitCascadingValue Computed<T>(Func<T> valueFactory, string? name = null, bool isFixed = false)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        return new(() => valueFactory(), true, typeof(T), name, isFixed, true);
    }

    /// <summary>
    /// Creates a computed cascading value with an explicit ValueType, for when the cascaded type of a value
    /// that is re-read on every render is only known at runtime.
    /// </summary>
    public static BitCascadingValue Computed(Func<object?> valueFactory, Type valueType, string? name = null, bool isFixed = false)
        => new(valueFactory, true, valueType, name, isFixed, true);

    /// <summary>
    /// Creates a cascading value with <see cref="AutoNotify"/> turned on, so a value that reports its own
    /// mutations through <see cref="INotifyCollectionChanged"/> or <see cref="INotifyPropertyChanged"/>
    /// refreshes the consumers without a single call to <see cref="NotifyChanged"/>.
    /// </summary>
    public static BitCascadingValue Observed<T>(T value, string? name = null, bool enabled = true)
        => new(value, name, false, typeof(T), enabled) { AutoNotify = true };



    public override string ToString()
    {
        // The stored reading is what is described, never the Value property, so that formatting this value -
        // a debugger evaluating it automatically, a log line interpolating it - never runs a user factory.
        object? snapshot;
        bool isCreated;
        bool isComputed;
        bool isComputedRead;

        lock (_lock)
        {
            snapshot = _value;
            isCreated = _valueFactory is null;
            isComputed = _computedFactory is not null;
            isComputedRead = _computedRead;
        }

        var value = isCreated is false ? "(not created yet)"
                  : isComputed && isComputedRead is false ? "(not read yet)"
                  : snapshot ?? "null";
        var flags = $"{(isComputed ? " (computed)" : string.Empty)}{(IsFixed ? " (fixed)" : string.Empty)}{(Enabled ? string.Empty : " (disabled)")}";

        return $"{(Name is null ? string.Empty : $"{Name}: ")}{ValueType.Name} = {value}{flags}";
    }



    /// <summary>
    /// Points the <see cref="AutoNotify"/> subscription at the value that is currently cascaded, and drops
    /// it entirely whenever the feature is off, nothing is listening, or the value is not created yet, so
    /// the cascaded object never holds on to a cascading value that no provider is using any more.
    /// </summary>
    private void UpdateObservation()
    {
        // The whole read-modify-apply is serialized, so that whichever caller decides what to detach and to
        // attach has applied it before the next one decides, which is what keeps a concurrent subscribe and
        // a concurrent value change from cancelling each other's subscription out.
        lock (_observationLock)
        {
            object? detach;
            object? attach;

            lock (_lock)
            {
                var hasListeners = _changed is not null || _changedAsync is not null;
                var target = _autoNotify && hasListeners && _valueFactory is null && _computedFactory is null ? _value : null;

                if (ReferenceEquals(_observedValue, target)) return;

                detach = _observedValue;
                attach = target;

                _observedValue = target;
            }

            // A collection is watched through INotifyCollectionChanged alone, even when it also reports the
            // property changes that come with it - an ObservableCollection<T> raises Count and Item[] beside
            // every collection change - so that one mutation asks for one refresh rather than three.
            if (detach is INotifyCollectionChanged detachedCollection)
            {
                detachedCollection.CollectionChanged -= HandleObservedCollectionChanged;
            }
            else if (detach is INotifyPropertyChanged detachedProperties)
            {
                detachedProperties.PropertyChanged -= HandleObservedPropertyChanged;
            }

            if (attach is INotifyCollectionChanged attachedCollection)
            {
                attachedCollection.CollectionChanged += HandleObservedCollectionChanged;
            }
            else if (attach is INotifyPropertyChanged attachedProperties)
            {
                attachedProperties.PropertyChanged += HandleObservedPropertyChanged;
            }
        }
    }

    private void HandleObservedPropertyChanged(object? sender, PropertyChangedEventArgs args) => NotifyChanged();

    private void HandleObservedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) => NotifyChanged();

    private static string? NormalizeName(string? name) => string.IsNullOrWhiteSpace(name) ? null : name;

    private static Type ValidateValueType(Type valueType)
    {
        if (valueType.ContainsGenericParameters)
        {
            throw new ArgumentException($"The open generic type '{valueType}' cannot be used as a cascading value type.", nameof(valueType));
        }

        if (valueType == typeof(void) || valueType.IsPointer || valueType.IsByRef || valueType.IsByRefLike)
        {
            throw new ArgumentException($"The type '{valueType}' cannot be used as a cascading value type.", nameof(valueType));
        }

        return valueType;
    }

    private static void ValidateValue(object? value, Type valueType)
    {
        if (value is null)
        {
            if (valueType.IsValueType && Nullable.GetUnderlyingType(valueType) is null)
            {
                throw new ArgumentException($"A null value cannot be cascaded as the non-nullable value type '{valueType}'. Provide a nullable valueType instead.", nameof(value));
            }

            return;
        }

        var type = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (type.IsInstanceOfType(value) is false)
        {
            throw new ArgumentException($"The provided value of type '{value.GetType()}' is not assignable to the cascading value type '{valueType}'.", nameof(value));
        }
    }



    public static implicit operator BitCascadingValue(bool value) => new(value);
    public static implicit operator BitCascadingValue((bool value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(bool? value) => new(value, typeof(bool?));
    public static implicit operator BitCascadingValue((bool? value, string name) tuple) => new(tuple.value, tuple.name, typeof(bool?));

    public static implicit operator BitCascadingValue(byte value) => new(value);
    public static implicit operator BitCascadingValue((byte value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(byte? value) => new(value, typeof(byte?));
    public static implicit operator BitCascadingValue((byte? value, string name) tuple) => new(tuple.value, tuple.name, typeof(byte?));

    public static implicit operator BitCascadingValue(sbyte value) => new(value);
    public static implicit operator BitCascadingValue((sbyte value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(sbyte? value) => new(value, typeof(sbyte?));
    public static implicit operator BitCascadingValue((sbyte? value, string name) tuple) => new(tuple.value, tuple.name, typeof(sbyte?));

    public static implicit operator BitCascadingValue(short value) => new(value);
    public static implicit operator BitCascadingValue((short value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(short? value) => new(value, typeof(short?));
    public static implicit operator BitCascadingValue((short? value, string name) tuple) => new(tuple.value, tuple.name, typeof(short?));

    public static implicit operator BitCascadingValue(ushort value) => new(value);
    public static implicit operator BitCascadingValue((ushort value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(ushort? value) => new(value, typeof(ushort?));
    public static implicit operator BitCascadingValue((ushort? value, string name) tuple) => new(tuple.value, tuple.name, typeof(ushort?));

    public static implicit operator BitCascadingValue(int value) => new(value);
    public static implicit operator BitCascadingValue((int value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(int? value) => new(value, typeof(int?));
    public static implicit operator BitCascadingValue((int? value, string name) tuple) => new(tuple.value, tuple.name, typeof(int?));

    public static implicit operator BitCascadingValue(uint value) => new(value);
    public static implicit operator BitCascadingValue((uint value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(uint? value) => new(value, typeof(uint?));
    public static implicit operator BitCascadingValue((uint? value, string name) tuple) => new(tuple.value, tuple.name, typeof(uint?));

    public static implicit operator BitCascadingValue(long value) => new(value);
    public static implicit operator BitCascadingValue((long value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(long? value) => new(value, typeof(long?));
    public static implicit operator BitCascadingValue((long? value, string name) tuple) => new(tuple.value, tuple.name, typeof(long?));

    public static implicit operator BitCascadingValue(ulong value) => new(value);
    public static implicit operator BitCascadingValue((ulong value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(ulong? value) => new(value, typeof(ulong?));
    public static implicit operator BitCascadingValue((ulong? value, string name) tuple) => new(tuple.value, tuple.name, typeof(ulong?));

    public static implicit operator BitCascadingValue(nint value) => new(value);
    public static implicit operator BitCascadingValue((nint value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(nint? value) => new(value, typeof(nint?));
    public static implicit operator BitCascadingValue((nint? value, string name) tuple) => new(tuple.value, tuple.name, typeof(nint?));

    public static implicit operator BitCascadingValue(nuint value) => new(value);
    public static implicit operator BitCascadingValue((nuint value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(nuint? value) => new(value, typeof(nuint?));
    public static implicit operator BitCascadingValue((nuint? value, string name) tuple) => new(tuple.value, tuple.name, typeof(nuint?));

    public static implicit operator BitCascadingValue(float value) => new(value);
    public static implicit operator BitCascadingValue((float value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(float? value) => new(value, typeof(float?));
    public static implicit operator BitCascadingValue((float? value, string name) tuple) => new(tuple.value, tuple.name, typeof(float?));

    public static implicit operator BitCascadingValue(double value) => new(value);
    public static implicit operator BitCascadingValue((double value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(double? value) => new(value, typeof(double?));
    public static implicit operator BitCascadingValue((double? value, string name) tuple) => new(tuple.value, tuple.name, typeof(double?));

    public static implicit operator BitCascadingValue(decimal value) => new(value);
    public static implicit operator BitCascadingValue((decimal value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(decimal? value) => new(value, typeof(decimal?));
    public static implicit operator BitCascadingValue((decimal? value, string name) tuple) => new(tuple.value, tuple.name, typeof(decimal?));

    public static implicit operator BitCascadingValue(char value) => new(value);
    public static implicit operator BitCascadingValue((char value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(char? value) => new(value, typeof(char?));
    public static implicit operator BitCascadingValue((char? value, string name) tuple) => new(tuple.value, tuple.name, typeof(char?));

    public static implicit operator BitCascadingValue(Guid value) => new(value);
    public static implicit operator BitCascadingValue((Guid value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(Guid? value) => new(value, typeof(Guid?));
    public static implicit operator BitCascadingValue((Guid? value, string name) tuple) => new(tuple.value, tuple.name, typeof(Guid?));

    public static implicit operator BitCascadingValue(DateTime value) => new(value);
    public static implicit operator BitCascadingValue((DateTime value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(DateTime? value) => new(value, typeof(DateTime?));
    public static implicit operator BitCascadingValue((DateTime? value, string name) tuple) => new(tuple.value, tuple.name, typeof(DateTime?));

    public static implicit operator BitCascadingValue(DateOnly value) => new(value);
    public static implicit operator BitCascadingValue((DateOnly value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(DateOnly? value) => new(value, typeof(DateOnly?));
    public static implicit operator BitCascadingValue((DateOnly? value, string name) tuple) => new(tuple.value, tuple.name, typeof(DateOnly?));

    public static implicit operator BitCascadingValue(TimeOnly value) => new(value);
    public static implicit operator BitCascadingValue((TimeOnly value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(TimeOnly? value) => new(value, typeof(TimeOnly?));
    public static implicit operator BitCascadingValue((TimeOnly? value, string name) tuple) => new(tuple.value, tuple.name, typeof(TimeOnly?));

    public static implicit operator BitCascadingValue(DateTimeOffset value) => new(value);
    public static implicit operator BitCascadingValue((DateTimeOffset value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(DateTimeOffset? value) => new(value, typeof(DateTimeOffset?));
    public static implicit operator BitCascadingValue((DateTimeOffset? value, string name) tuple) => new(tuple.value, tuple.name, typeof(DateTimeOffset?));

    public static implicit operator BitCascadingValue(TimeSpan value) => new(value);
    public static implicit operator BitCascadingValue((TimeSpan value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(TimeSpan? value) => new(value, typeof(TimeSpan?));
    public static implicit operator BitCascadingValue((TimeSpan? value, string name) tuple) => new(tuple.value, tuple.name, typeof(TimeSpan?));

    public static implicit operator BitCascadingValue(string? value) => new(value, typeof(string));
    public static implicit operator BitCascadingValue((string? value, string name) tuple) => new(tuple.value, tuple.name, typeof(string));

    public static implicit operator BitCascadingValue(BitDir value) => new(value);
    public static implicit operator BitCascadingValue((BitDir value, string name) tuple) => new(tuple.value, tuple.name);
    public static implicit operator BitCascadingValue(BitDir? value) => new(value, typeof(BitDir?));
    public static implicit operator BitCascadingValue((BitDir? value, string name) tuple) => new(tuple.value, tuple.name, typeof(BitDir?));

    public static implicit operator BitCascadingValue(RouteData? value) => new(value, typeof(RouteData));
    public static implicit operator BitCascadingValue((RouteData? value, string name) tuple) => new(tuple.value, tuple.name, typeof(RouteData));

    public static implicit operator BitCascadingValue(Uri? value) => new(value, typeof(Uri));
    public static implicit operator BitCascadingValue((Uri? value, string name) tuple) => new(tuple.value, tuple.name, typeof(Uri));

    public static implicit operator BitCascadingValue(CultureInfo? value) => new(value, typeof(CultureInfo));
    public static implicit operator BitCascadingValue((CultureInfo? value, string name) tuple) => new(tuple.value, tuple.name, typeof(CultureInfo));

    public static implicit operator BitCascadingValue(TimeZoneInfo? value) => new(value, typeof(TimeZoneInfo));
    public static implicit operator BitCascadingValue((TimeZoneInfo? value, string name) tuple) => new(tuple.value, tuple.name, typeof(TimeZoneInfo));
}
