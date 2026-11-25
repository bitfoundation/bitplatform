using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Bit.BlazorUI;

// The interop of the keyboard with its script (BitVirtualKeyboard.ts): the options it is told, the version the keys are
// rendered with (so a press between a render and the script's update is left alone), and what the script tells back.
public partial class BitVirtualKeyboard
{
    private DotNetObjectReference<BitVirtualKeyboard>? _dotnetObj;
    private IJSObjectReference? _handle;
    private bool _settingUp;

    // The state the script was last given, compared by value (the compiled layout by reference, which the compiler's
    // cache keeps for a layout of the same content), and the version it was rendered with.
    private ScriptState? _scriptState;
    private int _payloadVersion;

    // The version of the key table alone, which the keys are rendered with: a press is left alone only while the keys on
    // the page are newer than the table the script holds, never while a value or an open state is on its way.
    private int _keysVersion;
    private int _sentVersion = -1;

    // The Target parameter the script was last handed. AttachAsync and DetachAsync leave it alone, so the render they cause
    // never hands the script a Target that has not changed and takes their input away from it.
    private string? _sentTargetId;

    // What the script last reported of the input it types into, and of the Value it holds.
    private BitVirtualKeyboardJsTargetInfo? _target;
    private string? _lastJsValue;
    private long _lastJsSeq;

    private int? _scrambleSeed;

    private sealed record ScriptState(BitVirtualKeyboardCompiledLayout Compiled, BitVirtualKeyboardStrings Strings, bool Docked, bool Open, bool ValueMode,
                                      bool FollowFocus, string? TargetSelector, BitVirtualKeyboardEnterBehavior EnterBehavior, string? AllowedCharacters,
                                      int? MaxLength, bool Secure, bool ShowKeyPreview, bool Sound, bool Haptics, bool HasKeyPress, bool HasInput,
                                      int LongPressDelay, int MultiTapTimeout, int RepeatDelay, int RepeatInterval, bool AutoForm, bool Compact,
                                      double CompactBreakpoint, bool ReserveSpace, bool Disabled, bool SetValue, string? Value, long ValueSeq,
                                      string? ActiveKeyClass, string? ActiveKeyStyle, string? PopupKeyClass, string? PopupKeyStyle);



    /// <summary>
    /// Acts as if the key were pressed: a key of the layer shown with this id (its bag id, its built-in name, or the text
    /// it types), else one of any layer.
    /// </summary>
    public async ValueTask PressKeyAsync(string keyId)
    {
        if (_handle is null || IsDisposed) return;
        await _handle.InvokeVoidAsync("press", keyId);
    }

    /// <summary>
    /// Shows a layer of the layout.
    /// </summary>
    public async ValueTask SetLayerAsync(string layerId)
    {
        if (_handle is null || IsDisposed) return;
        await _handle.InvokeVoidAsync("setLayer", layerId);
    }

    /// <summary>
    /// Types into an input or textarea from now on.
    /// </summary>
    public async ValueTask AttachAsync(ElementReference input)
    {
        if (_handle is null || IsDisposed) return;
        await _handle.InvokeVoidAsync("attach", input);
    }

    /// <summary>
    /// Stops typing into the input the keyboard was attached to.
    /// </summary>
    public async ValueTask DetachAsync()
    {
        if (_handle is null || IsDisposed) return;
        await _handle.InvokeVoidAsync("attach", (object?)null);
    }



    [JSInvokable("OnKeyPress")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitVirtualKeyboardKeyPressArgs))]
    public async Task<BitVirtualKeyboardKeyPressArgs> _OnKeyPress(string keyId, string action, string? text, string layer)
    {
        var keyPress = new BitVirtualKeyboardKeyPressArgs
        {
            KeyId = keyId,
            Action = Enum.TryParse<BitVirtualKeyboardKeyAction>(action, out var parsed) ? parsed : BitVirtualKeyboardKeyAction.Character,
            Text = text,
            Layer = layer,
        };

        if (IsDisposing) return keyPress;

        await OnKeyPress.InvokeAsync(keyPress);

        return keyPress;
    }
    [JSInvokable("OnValue")]
    public async Task _OnValue(string? value, long seq)
    {
        if (IsDisposing) return;

        _lastJsValue = value;
        _lastJsSeq = seq;

        // A Value the parent holds without a change callback stays where the parent holds it: the script is given it back
        // by the render that follows.
        await AssignValue(value);

        if (IsSecure is false) await OnInput.InvokeAsync(value);

        RefreshAndRender();
    }

    [JSInvokable("OnTargetValue")]
    public async Task _OnTargetValue(string? value)
    {
        if (IsDisposing || IsSecure) return;

        await OnInput.InvokeAsync(value);
    }

    [JSInvokable("OnLayer")]
    public async Task _OnLayer(string layer)
    {
        if (IsDisposing) return;

        _activeLayer = layer;
        await OnLayerChange.InvokeAsync(layer);
    }

    [JSInvokable("OnTarget")]
    public async Task _OnTarget(string? type, string? inputMode, string? enterKeyHint, string? purpose, bool password, string? id)
    {
        if (IsDisposing) return;

        var target = type is null ? null : new BitVirtualKeyboardJsTargetInfo
        {
            Id = id,
            Type = type,
            InputMode = inputMode,
            EnterKeyHint = enterKeyHint,
            Purpose = purpose,
            Password = password,
        };

        _target = target;
        if (Scramble && target is not null) _scrambleSeed = RandomNumberGenerator.GetInt32(int.MaxValue);

        await RecompileAndRender();

        // the names of the keys follow the input too (the Enter of an enterkeyhint), whatever the layout does
        StateHasChanged();
    }

    [JSInvokable("OnEnter")]
    public async Task _OnEnter()
    {
        if (IsDisposing) return;

        await OnEnter.InvokeAsync();
    }

    [JSInvokable("OnCommand")]
    public async Task _OnCommand(string command, string keyId)
    {
        if (IsDisposing) return;

        await OnCommand.InvokeAsync(new() { Command = command, KeyId = keyId });
    }

    [JSInvokable("OnOpen")]
    public async Task _OnOpen(bool open)
    {
        if (IsDisposing) return;

        if (open && Scramble) _scrambleSeed = RandomNumberGenerator.GetInt32(int.MaxValue);

        var assigned = await AssignOpen(open);

        await RecompileAndRender();

        // An Open the parent holds one way stays as it is: the script is given it back by a render of its own, since
        // nothing the script is told has changed on this side.
        if (assigned is false)
        {
            _payloadVersion++;
            StateHasChanged();
        }
    }

    [JSInvokable("OnCompact")]
    public async Task _OnCompact(bool compact)
    {
        if (IsDisposing) return;

        _compact = compact;
        await RecompileAndRender();
    }

    [JSInvokable("OnNextLayout")]
    public async Task _OnNextLayout()
    {
        if (IsDisposing || Languages is not { Count: > 1 } languages) return;

        _languageIndex = (_languageIndex + 1) % languages.Count;
        await RecompileAndRender();
    }



    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed || _settingUp) return;

        try
        {
            if (_handle is null)
            {
                _settingUp = true;
                _dotnetObj ??= DotNetObjectReference.Create(this);
                _sentVersion = _payloadVersion;
                _sentTargetId = Target?.Id;

                var handle = await _js.BitVirtualKeyboardSetup(RootElement, _dotnetObj, BuildOptions(), Target);
                _settingUp = false;

                // disposed while the script was being set up: it is let go of at once
                if (IsDisposed)
                {
                    await DisposeHandle(handle);
                    return;
                }

                _handle = handle;
            }

            if (_sentVersion != _payloadVersion)
            {
                _sentVersion = _payloadVersion;
                await _handle.InvokeVoidAsync("update", BuildOptions());

                // disposed while the update was on its way: the handle is gone
                if (IsDisposed || _handle is null) return;
            }

            if (Target?.Id != _sentTargetId)
            {
                _sentTargetId = Target?.Id;
                await _handle.InvokeVoidAsync("attach", Target);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        finally
        {
            _settingUp = false;
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        var handle = _handle;
        _handle = null;

        await _dotnetObj.DisposeAfterAsync(() => DisposeHandle(handle));

        await base.DisposeAsync(disposing);
    }

    private static async ValueTask DisposeHandle(IJSObjectReference? handle)
    {
        if (handle is null) return;

        try
        {
            await handle.InvokeVoidAsync("dispose");
            await handle.DisposeAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (JSException) { } // nor a page that has already torn the script down
    }



    private bool IsSecure => Secure || _target?.Password is true;

    private bool IsNumberTarget => _target?.Type == "number";

    // What is being typed: the Purpose parameter, else what the input typed into says (its data-bit-vkb, its original
    // inputmode, its type), else Text.
    private BitVirtualKeyboardPurpose ResolvePurpose()
    {
        if (Purpose.HasValue) return Purpose.Value;

        var target = _target;
        if (target is null) return BitVirtualKeyboardPurpose.Text;

        if (target.Purpose is { Length: > 0 } named
            && Enum.TryParse<BitVirtualKeyboardPurpose>(named.Replace("-", ""), ignoreCase: true, out var fromAttribute))
        {
            return fromAttribute;
        }

        var fromMode = target.InputMode?.ToLowerInvariant() switch
        {
            "tel" => BitVirtualKeyboardPurpose.Tel,
            "numeric" => BitVirtualKeyboardPurpose.Numeric,
            "decimal" => BitVirtualKeyboardPurpose.Decimal,
            "email" => BitVirtualKeyboardPurpose.Email,
            "url" => BitVirtualKeyboardPurpose.Url,
            "search" => BitVirtualKeyboardPurpose.Search,
            "text" => BitVirtualKeyboardPurpose.Text,
            _ => (BitVirtualKeyboardPurpose?)null,
        };
        if (fromMode.HasValue) return fromMode.Value;

        return target.Type?.ToLowerInvariant() switch
        {
            "tel" => BitVirtualKeyboardPurpose.Tel,
            "email" => BitVirtualKeyboardPurpose.Email,
            "url" => BitVirtualKeyboardPurpose.Url,
            "search" => BitVirtualKeyboardPurpose.Search,
            "number" => BitVirtualKeyboardPurpose.Decimal,
            _ => BitVirtualKeyboardPurpose.Text,
        };
    }

    private async Task RecompileAndRender()
    {
        var before = _compiled;
        Compile();
        RefreshAndRender();

        if (before is not null && ReferenceEquals(before.Source, _compiled.Source) is false && before.Id != _compiled.Id)
        {
            await OnLayoutChange.InvokeAsync(_compiled.Source);
        }
    }

    private void RefreshAndRender()
    {
        var version = _payloadVersion;
        RefreshScriptState();
        if (version != _payloadVersion) StateHasChanged();
    }

    // A new version for every change of what the script is told; the keys are rendered with it.
    private void RefreshScriptState()
    {
        var state = new ScriptState(
            _compiled, ActiveStrings, Mode is BitVirtualKeyboardMode.Docked, Open, IsValueMode, FollowFocus, TargetSelector, EnterBehavior,
            AllowedCharacters, MaxLength, IsSecure, ShowKeyPreview, Sound, Haptics, OnKeyPress.HasDelegate, OnInput.HasDelegate,
            LongPressDelay, MultiTapTimeout, RepeatDelay, RepeatInterval, Form is BitVirtualKeyboardForm.Auto, _compact,
            CompactBreakpoint, ReserveSpace, Disabled, Value != _lastJsValue, Value, _lastJsSeq,
            Classes?.ActiveKey, Styles?.ActiveKey, Classes?.PopupKey, Styles?.PopupKey);

        if (state == _scriptState) return;

        if (ReferenceEquals(state.Compiled, _scriptState?.Compiled) is false) _keysVersion++;
        _scriptState = state;
        _payloadVersion++;
    }

    private BitVirtualKeyboardJsOptions BuildOptions()
    {
        var state = _scriptState!;
        var compiled = state.Compiled;

        return new()
        {
            Version = _payloadVersion,
            KeysVersion = _keysVersion,
            LayoutId = compiled.Id,
            BaseLayer = compiled.Layers[0].Id,
            ShiftLayer = compiled.ShiftLayer,
            CapsLayer = compiled.CapsLayer,
            AltGrLayer = compiled.AltGrLayer,
            ShiftAltGrLayer = compiled.ShiftAltGrLayer,
            Keys = [.. compiled.Layers.SelectMany(layer => layer.Keys.Select(key => new BitVirtualKeyboardJsKey
            {
                Id = key.KeyId,
                Ly = layer.Id,
                A = key.Gap || key.Blank ? "None" : key.Action.ToString(),
                O = key.Output,
                L = key.Layer,
                Nl = key.NextLayer,
                Dk = key.DeadKey,
                C = key.Command,
                Lp = key.LongPress.Count > 0 ? key.LongPress : null,
                Lpd = key.LongPressDefault,
                Mt = key.MultiTap.Count > 0 ? key.MultiTap : null,
                R = key.Repeat,
                P = key.Preview,
            }))],
            DeadKeys = compiled.DeadKeys,
            Docked = state.Docked,
            Open = state.Open,
            ValueMode = state.ValueMode,
            FollowFocus = state.FollowFocus,
            TargetSelector = state.TargetSelector,
            EnterBehavior = state.EnterBehavior.ToString(),
            AllowedCharacters = state.AllowedCharacters,
            MaxLength = state.MaxLength,
            Secure = state.Secure,
            ShowKeyPreview = state.ShowKeyPreview,
            Sound = state.Sound,
            Haptics = state.Haptics,
            HasKeyPressHandler = state.HasKeyPress,
            HasInputHandler = state.HasInput,
            LongPressDelay = state.LongPressDelay,
            MultiTapTimeout = state.MultiTapTimeout,
            RepeatDelay = state.RepeatDelay,
            RepeatInterval = state.RepeatInterval,
            AutoForm = state.AutoForm,
            Compact = state.Compact,
            CompactBreakpoint = state.CompactBreakpoint,
            ReserveSpace = state.ReserveSpace,
            Disabled = state.Disabled,
            SetValue = state.SetValue,
            Value = state.Value,
            ValueSeq = state.ValueSeq,
            ActiveKeyClass = state.ActiveKeyClass,
            ActiveKeyStyle = state.ActiveKeyStyle,
            PopupKeyClass = state.PopupKeyClass,
            PopupKeyStyle = state.PopupKeyStyle,
            Strings = new()
            {
                Alternates = ActiveStrings.Alternates,
                LayerAnnouncement = ActiveStrings.LayerAnnouncement,
                ShiftOn = ActiveStrings.ShiftOn,
                ShiftLocked = ActiveStrings.ShiftLocked,
                ShiftOff = ActiveStrings.ShiftOff,
                CapsLockOn = ActiveStrings.CapsLockOn,
                CapsLockOff = ActiveStrings.CapsLockOff,
                LayerNames = compiled.Layers.ToDictionary(layer => layer.Id, GetLayerName),
            },
        };
    }
}
