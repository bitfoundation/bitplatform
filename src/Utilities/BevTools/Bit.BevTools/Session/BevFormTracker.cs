using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BevTools;

/// <summary>One field of a form as its <see cref="EditContext"/> knows it. Its value is never read.</summary>
/// <param name="Name">The field ("Email"), with its model type when it is not the form's model ("Address.City").</param>
/// <param name="Modified">Whether the EditContext marks it modified.</param>
/// <param name="Changes">How many times it notified a change while BevTools watched the form.</param>
/// <param name="Messages">Its validation messages: redacted, and hidden under <see cref="BevValueAccess.None"/> (a parse error can repeat what was typed).</param>
public sealed record BevFormField(string Name, bool Modified, int Changes, IReadOnlyList<string> Messages);

/// <summary>A live form - an <see cref="EditContext"/> - and what BevTools observed of it.</summary>
/// <param name="Id">BevTools' number for the form in this session.</param>
/// <param name="ModelType">The type of the form's model.</param>
/// <param name="OwnerInstanceId">The component the form is on: the one rendering its EditForm.</param>
/// <param name="OwnerName">That component's name.</param>
/// <param name="Modified">Whether any field is marked modified.</param>
/// <param name="MessageCount">How many validation messages the form holds now.</param>
/// <param name="Fields">Its fields: the ones the EditContext tracks, and the ones that notified a change.</param>
/// <param name="Validations">How many times the form was validated while BevTools watched it.</param>
/// <param name="LastValidatedAt">When it was last validated.</param>
/// <param name="LastValidationPassed">Whether that validation left no message; null before the first.</param>
/// <param name="Validators">What answers its validation requests (<c>DataAnnotationsValidator</c>, a custom validator); empty when nothing does.</param>
/// <param name="MessageDisplays">The <c>ValidationSummary</c> and <c>ValidationMessage</c> components listening to it.</param>
/// <param name="ValidationAttributes">The model's members carrying validation attributes ("Email: Required, EmailAddress").</param>
public sealed record BevFormInfo(
    int Id,
    string ModelType,
    long? OwnerInstanceId,
    string? OwnerName,
    IReadOnlyList<string> Validators,
    IReadOnlyList<string> MessageDisplays,
    bool Modified,
    int MessageCount,
    IReadOnlyList<BevFormField> Fields,
    int Validations,
    DateTimeOffset? LastValidatedAt,
    bool? LastValidationPassed,
    IReadOnlyList<string> ValidationAttributes)
{
    /// <summary>
    /// The EditForm has an <c>OnSubmit</c> handler: the code decides when and how the form is validated (it may call
    /// <c>Validator.TryValidateObject</c> itself), so no validator component is not a defect.
    /// </summary>
    public bool HandlesSubmit { get; init; }
}

/// <summary>
/// Finds the session's forms - the EditContexts of its <c>EditForm</c>s and input components - and watches them: which
/// fields change, when the form is validated and with what outcome, and what validates it and shows its messages. Field
/// values are never read. An EditForm's form is watched from its first render (and again when it gets a new
/// EditContext), others when the tree is next read. EditContexts are held weakly - nothing BevTools keeps refers to one
/// - and a form is forgotten, its handlers removed, once the EditForm or input it was found through is disposed.
/// BevTools' own handlers on the form's events are left out of everything it reports about those events, and a
/// validation's outcome is read after every validator ran.
/// </summary>
internal sealed class BevFormTracker(BevToolsSession session)
{
    private const int MaxForms = 200;
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo? EditFormContextField = typeof(EditForm).GetField("_editContext", Members);
    private static readonly FieldInfo? FieldStatesField = typeof(EditContext).GetField("_fieldStates", Members);
    private static readonly FieldInfo? ValidationRequestedField = typeof(EditContext).GetField(nameof(EditContext.OnValidationRequested), Members);
    private static readonly FieldInfo? ValidationStateChangedField = typeof(EditContext).GetField(nameof(EditContext.OnValidationStateChanged), Members);
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> InputContextProperties = new();
    private static readonly ConcurrentDictionary<Type, string[]> ValidatedMembers = new();

    private readonly object _lock = new();
    private readonly List<Form> _forms = [];
    private int _nextId;

    /// <summary>More forms were alive at once than BevTools watches (200): the newest are not listed.</summary>
    public bool Truncated { get; private set; }

    private sealed class Form(int id, EditContext context, BevComponentRecord holder)
    {
        private readonly WeakReference<EditContext> _context = new(context);

        public int Id { get; } = id;

        /// <summary>The EditForm (or input) the form was found through; the page or component it is on is resolved from it when asked, once the tree knows its parents.</summary>
        public BevComponentRecord Holder { get; } = holder;

        public BevComponentRecord Owner => Holder.Component is EditForm
            ? NearestApplicationComponent(Holder) ?? Holder
            : NearestForm(Holder) ?? NearestApplicationComponent(Holder) ?? Holder;

        public EditContext? Context => _context.TryGetTarget(out var c) ? c : null;

        public Dictionary<string, int> Changes { get; } = new(StringComparer.Ordinal);

        public int Validations;

        public DateTimeOffset? LastValidatedAt;

        public bool? LastValidationPassed;

        public EventHandler<ValidationRequestedEventArgs>? OnValidationRequested;

        public EventHandler<FieldChangedEventArgs>? OnFieldChanged;

        /// <summary>The form's page or input went away (or the context was collected): nothing left to describe.</summary>
        public bool IsGone => Holder.IsDisposed || Context is null;
    }

    /// <summary>Whether the reflection forms are read through exists on this framework version.</summary>
    public static bool IsAvailable => EditFormContextField is not null && FieldStatesField is not null && ValidationRequestedField is not null;

    /// <summary>Finds forms that appeared since the last call and forgets the ones that are gone. Runs on the renderer's dispatcher.</summary>
    public void Discover()
    {
        if (!session.IsEnabled)
        {
            return;
        }

        foreach (var record in session.Components.All)
        {
            if (record.IsDisposed || record.IsBevTools || record.Component is not { } component || ContextOf(component) is not { } context)
            {
                continue;
            }

            lock (_lock)
            {
                if (_forms.Exists(f => ReferenceEquals(f.Context, context)))
                {
                    continue;
                }
            }

            Watch(context, record);
        }

        Prune();
    }

    public BevFormInfo[] Forms()
    {
        Prune();
        List<Form> forms;
        lock (_lock)
        {
            forms = [.. _forms];
        }

        var result = new List<BevFormInfo>();
        foreach (var form in forms)
        {
            if (!form.IsGone && form.Context is { } context)
            {
                result.Add(Describe(form, context));
            }
        }

        return [.. result];
    }

    /// <summary>Forgets the forms whose page or input is gone, and takes BevTools' handlers off their EditContexts.</summary>
    private void Prune()
    {
        List<Form> gone;
        lock (_lock)
        {
            gone = _forms.Where(f => f.IsGone).ToList();
            _forms.RemoveAll(f => f.IsGone);
        }

        foreach (var form in gone)
        {
            Unsubscribe(form);
        }
    }

    private static void Unsubscribe(Form form)
    {
        if (form.Context is not { } context)
        {
            return;
        }

        try
        {
            context.OnValidationRequested -= form.OnValidationRequested;
            context.OnFieldChanged -= form.OnFieldChanged;
        }
        catch
        {
        }
    }

    private static EditContext? ContextOf(IComponent component)
    {
        try
        {
            if (component is EditForm form)
            {
                return form.EditContext ?? EditFormContextField?.GetValue(form) as EditContext;
            }

            var property = InputContextProperties.GetOrAdd(component.GetType(), static type =>
            {
                for (var t = type; t is not null && t != typeof(ComponentBase); t = t.BaseType)
                {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(InputBase<>))
                    {
                        return t.GetProperty("EditContext", Members);
                    }
                }

                return null;
            });
            return property?.GetValue(component) as EditContext;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The application component that renders an EditForm: forms are named after the page or component they are on.</summary>
    private static BevComponentRecord? NearestApplicationComponent(BevComponentRecord record)
    {
        for (var current = record.Parent; current is not null; current = current.Parent)
        {
            if (!current.IsHidden && !current.IsBevTools)
            {
                return current;
            }
        }

        return null;
    }

    /// <summary>An input's form is the EditForm above it, and that form's owner is the component it is on.</summary>
    private static BevComponentRecord? NearestForm(BevComponentRecord record)
    {
        for (var current = record.Parent; current is not null; current = current.Parent)
        {
            if (current.Component is EditForm)
            {
                return NearestApplicationComponent(current) ?? current;
            }
        }

        return null;
    }

    /// <summary>The EditContext an EditForm uses: its own parameter, or the one it built for its Model.</summary>
    internal static EditContext? ContextOfForm(EditForm form) => ContextOf(form);

    /// <summary>Starts watching an EditForm's form from its first render (called by the render tracker).</summary>
    public void Watch(EditForm editForm, BevComponentRecord record)
    {
        if (session.IsEnabled && ContextOf(editForm) is { } context)
        {
            Watch(context, record);
        }
    }

    private void Watch(EditContext context, BevComponentRecord holder)
    {
        Form form;
        List<Form> replaced;
        lock (_lock)
        {
            if (_forms.Exists(f => ReferenceEquals(f.Context, context)))
            {
                return;
            }

            // The same EditForm with a new EditContext (a form reset after submit: a new model) is a new form; the old
            // context is no longer the form's.
            replaced = _forms.Where(f => ReferenceEquals(f.Holder, holder) || f.IsGone).ToList();
            _forms.RemoveAll(f => ReferenceEquals(f.Holder, holder) || f.IsGone);
            if (_forms.Count >= MaxForms)
            {
                Truncated = true;
                return;
            }

            form = new Form(++_nextId, context, holder);
            _forms.Add(form);
        }

        foreach (var old in replaced)
        {
            Unsubscribe(old);
        }

        // Validators run in the order they subscribed, and a form is watched from its first render - before the validator
        // components inside it initialize. Validation itself is synchronous (EditContext.Validate raises the event and
        // then reads the messages), so the outcome is read once the current work yields: after every validator ran.
        form.OnValidationRequested = (sender, args) =>
        {
            var trigger = session.CurrentTriggerEventId;
            if (sender is EditContext edited && IsLastHandler(edited, form.OnValidationRequested))
            {
                // Every validator ran before this handler: the outcome is exact, before OnValidSubmit adds anything.
                OnValidated(form, trigger);
            }
            else
            {
                // Validators subscribed after BevTools (they are the form's children): read once they ran, and move to
                // the end of the list so the next validation is read exactly.
                if (sender is EditContext requested)
                {
                    MoveToEnd(requested, form.OnValidationRequested);
                }

                _ = ReadOutcomeAsync(form, trigger);
            }
        };

        // The context comes as the sender: a closure over it would keep every form alive in BevTools' list.
        form.OnFieldChanged = (sender, e) =>
        {
            if (sender is not EditContext edited)
            {
                return;
            }

            lock (form.Changes)
            {
                var name = FieldName(edited.Model, e.FieldIdentifier);
                form.Changes[name] = form.Changes.GetValueOrDefault(name) + 1;
            }
        };

        try
        {
            context.OnValidationRequested += form.OnValidationRequested;
            context.OnFieldChanged += form.OnFieldChanged;
        }
        catch
        {
            // Looking must never change what the form does; a form that cannot be watched is still described.
        }
    }

    private static bool IsLastHandler(EditContext context, Delegate? own)
    {
        try
        {
            return own is not null && ValidationRequestedField?.GetValue(context) is Delegate handlers && handlers.GetInvocationList()[^1].Equals(own);
        }
        catch
        {
            return false;
        }
    }

    private static void MoveToEnd(EditContext context, EventHandler<ValidationRequestedEventArgs>? own)
    {
        try
        {
            context.OnValidationRequested -= own;
            context.OnValidationRequested += own;
        }
        catch
        {
        }
    }

    private async Task ReadOutcomeAsync(Form form, long? trigger)
    {
        await Task.Yield();
        OnValidated(form, trigger);
    }

    private void OnValidated(Form form, long? trigger)
    {
        try
        {
            if (form.Context is not { } context)
            {
                return;
            }

            var messages = context.GetValidationMessages().Count();
            Interlocked.Increment(ref form.Validations);
            form.LastValidatedAt = DateTimeOffset.UtcNow;
            form.LastValidationPassed = messages == 0;
            var model = TypeNames.Short(context.Model.GetType());
            var owner = form.Owner;
            session.Timeline.Record(new BevToolsEvent
            {
                Kind = BevToolsEventKind.Custom,
                Category = "form",
                Title = messages == 0 ? "Form validated: " + model + " is valid" : "Form validated: " + model + ", " + messages + " message" + (messages == 1 ? "" : "s"),
                Detail = HasValidator(context, form) ? "on " + owner.DisplayName : "on " + owner.DisplayName + "; nothing validates it (no validator component listens)",
                Severity = messages == 0 ? BevToolsSeverity.Info : BevToolsSeverity.Warning,
                ComponentInstanceId = owner.InstanceId,
                ComponentName = owner.DisplayName,
                ParentEventId = trigger,
            });
            session.Touch();
        }
        catch
        {
            // Never into the app's validation.
        }
    }

    private BevFormInfo Describe(Form form, EditContext context)
    {
        var validators = Validators(context, form).Select(DescribeValidator).Distinct().ToArray();
        var displays = Handlers(ValidationStateChangedField, context, null)
            .Select(h => h.Target)
            .Where(t => t is ValidationSummary || (t is not null && IsValidationMessage(t.GetType())))
            .Select(t => TypeNames.Short(t!.GetType()))
            .ToArray();
        var fields = new List<BevFormField>();
        var messageCount = 0;
        Dictionary<string, int> changes;
        lock (form.Changes)
        {
            changes = new Dictionary<string, int>(form.Changes, StringComparer.Ordinal);
        }

        foreach (var field in FieldIdentifiers(context).Take(100))
        {
            var name = FieldName(context.Model, field);
            var messages = context.GetValidationMessages(field).Take(20).Select(Message).ToArray();
            messageCount += messages.Length;
            fields.Add(new BevFormField(name, SafeIsModified(context, field), changes.GetValueOrDefault(name), messages));
            changes.Remove(name);
        }

        // Fields that changed but have no state (yet): an EditContext creates field states lazily.
        fields.AddRange(changes.Select(c => new BevFormField(c.Key, true, c.Value, [])));
        var formMessages = context.GetValidationMessages().Count();
        if (formMessages > messageCount)
        {
            fields.Add(new BevFormField("(form)", false, 0, context.GetValidationMessages(new FieldIdentifier(context.Model, string.Empty)).Take(20).Select(Message).ToArray()));
        }

        var owner = form.Owner;
        return new BevFormInfo(
            form.Id,
            TypeNames.Short(context.Model.GetType()),
            owner.InstanceId,
            owner.DisplayName,
            validators,
            displays,
            SafeIsModified(context, null),
            formMessages,
            fields,
            Volatile.Read(ref form.Validations),
            form.LastValidatedAt,
            form.LastValidationPassed,
            ValidatedMembers.GetOrAdd(context.Model.GetType(), ValidationAttributesOf))
        {
            HandlesSubmit = form.Holder.Component is EditForm editForm && editForm.OnSubmit.HasDelegate,
        };
    }

    /// <summary>A ValidationMessage&lt;T&gt;, or a component derived from one.</summary>
    private static bool IsValidationMessage(Type type)
    {
        for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ValidationMessage<>))
            {
                return true;
            }
        }

        return false;
    }

    private string Message(string message) => session.ValueAccess == BevValueAccess.None
        ? BevRedactor.HiddenValue
        : BevRedactor.RedactText(message.Length > 200 ? message[..200] + "…" : message);

    private static bool SafeIsModified(EditContext context, FieldIdentifier? field)
    {
        try
        {
            return field is { } f ? context.IsModified(f) : context.IsModified();
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<FieldIdentifier> FieldIdentifiers(EditContext context)
    {
        try
        {
            return FieldStatesField?.GetValue(context) is IDictionary states ? states.Keys.OfType<FieldIdentifier>().ToArray() : [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>"Email", or "Address.City" for a field of a nested model.</summary>
    private static string FieldName(object model, FieldIdentifier field) =>
        ReferenceEquals(field.Model, model) ? field.FieldName : TypeNames.Short(field.Model.GetType()) + "." + field.FieldName;

    /// <summary>The handlers of one of the EditContext's events, BevTools' own left out.</summary>
    private static Delegate[] Handlers(FieldInfo? eventField, EditContext context, Delegate? own)
    {
        try
        {
            return eventField?.GetValue(context) is Delegate handlers
                ? handlers.GetInvocationList().Where(h => own is null || !h.Equals(own)).Where(h => !IsBevTools(h)).ToArray()
                : [];
        }
        catch
        {
            return [];
        }
    }

    private static bool IsBevTools(Delegate handler) => handler.Method.DeclaringType?.Assembly == typeof(BevFormTracker).Assembly;

    private static bool HasValidator(EditContext context, Form form) => Validators(context, form).Length > 0;

    /// <summary>
    /// What validates the model: the handlers of OnValidationRequested, without BevTools' own and without the framework's
    /// form mapping (an EditForm with a FormName on a server subscribes to report form-post binding errors; it never
    /// checks the model's attributes).
    /// </summary>
    private static Delegate[] Validators(EditContext context, Form form) =>
        Handlers(ValidationRequestedField, context, form.OnValidationRequested)
            .Where(h => h.Method.DeclaringType?.FullName?.StartsWith("Microsoft.AspNetCore.Components.Forms.Mapping.", StringComparison.Ordinal) != true)
            .ToArray();

    /// <summary>
    /// "DataAnnotationsValidator" for the framework's own subscription, "ObjectGraphDataAnnotationsValidator" for the
    /// nested-model one, otherwise the type that subscribed - the type that declared a lambda, never its closure class.
    /// </summary>
    private static string DescribeValidator(Delegate handler)
    {
        var declaring = handler.Method.DeclaringType;
        while (declaring is { DeclaringType: not null } && (declaring.Name.StartsWith('<') || declaring.IsDefined(typeof(CompilerGeneratedAttribute), false)))
        {
            declaring = declaring.DeclaringType;
        }

        var name = declaring is null ? "?" : declaring.FullName ?? declaring.Name;
        if (name.Contains("ObjectGraphDataAnnotationsValidator", StringComparison.Ordinal))
        {
            return "ObjectGraphDataAnnotationsValidator";
        }

        if (name.Contains("DataAnnotations", StringComparison.Ordinal))
        {
            return "DataAnnotationsValidator";
        }

        var target = handler.Target?.GetType();
        return TypeNames.Short(target is not null && !target.IsDefined(typeof(CompilerGeneratedAttribute), false) ? target : declaring ?? typeof(object));
    }

    private static string[] ValidationAttributesOf(Type model)
    {
        try
        {
            return model.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(p => (p.Name, Attributes: p.GetCustomAttributes<ValidationAttribute>(inherit: true).Select(a => a.GetType().Name.Replace("Attribute", "", StringComparison.Ordinal)).Distinct().ToArray()))
                .Where(p => p.Attributes.Length > 0)
                .Select(p => p.Name + ": " + string.Join(", ", p.Attributes))
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            foreach (var form in _forms)
            {
                lock (form.Changes)
                {
                    form.Changes.Clear();
                }

                form.Validations = 0;
                form.LastValidatedAt = null;
                form.LastValidationPassed = null;
            }
        }
    }
}
