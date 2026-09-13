namespace Bit.Butil;

/// <summary>
/// The <c>KeyboardEvent.code</c> values <see cref="Keyboard"/> shortcuts are registered against.
/// <para>
/// A code names a physical key position, not the character it produces: <see cref="KeyA"/> is the
/// key where <c>A</c> sits on a US layout, and it stays <see cref="KeyA"/> on an AZERTY keyboard
/// where that key prints <c>Q</c>. That is what makes it the right thing to match a shortcut on -
/// Ctrl+<see cref="KeyZ"/> lands under the same finger everywhere - and the wrong thing to build
/// text from.
/// </para>
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/code">KeyboardEvent.code</see>
/// </summary>
public static class ButilKeyCodes
{
    // Letters
    /// <summary>The <c>KeyA</c> code - the key that prints <c>A</c> on a US layout.</summary>
    public const string KeyA = "KeyA";
    /// <summary>The <c>KeyB</c> code - the key that prints <c>B</c> on a US layout.</summary>
    public const string KeyB = "KeyB";
    /// <summary>The <c>KeyC</c> code - the key that prints <c>C</c> on a US layout.</summary>
    public const string KeyC = "KeyC";
    /// <summary>The <c>KeyD</c> code - the key that prints <c>D</c> on a US layout.</summary>
    public const string KeyD = "KeyD";
    /// <summary>The <c>KeyE</c> code - the key that prints <c>E</c> on a US layout.</summary>
    public const string KeyE = "KeyE";
    /// <summary>The <c>KeyF</c> code - the key that prints <c>F</c> on a US layout.</summary>
    public const string KeyF = "KeyF";
    /// <summary>The <c>KeyG</c> code - the key that prints <c>G</c> on a US layout.</summary>
    public const string KeyG = "KeyG";
    /// <summary>The <c>KeyH</c> code - the key that prints <c>H</c> on a US layout.</summary>
    public const string KeyH = "KeyH";
    /// <summary>The <c>KeyI</c> code - the key that prints <c>I</c> on a US layout.</summary>
    public const string KeyI = "KeyI";
    /// <summary>The <c>KeyJ</c> code - the key that prints <c>J</c> on a US layout.</summary>
    public const string KeyJ = "KeyJ";
    /// <summary>The <c>KeyK</c> code - the key that prints <c>K</c> on a US layout.</summary>
    public const string KeyK = "KeyK";
    /// <summary>The <c>KeyL</c> code - the key that prints <c>L</c> on a US layout.</summary>
    public const string KeyL = "KeyL";
    /// <summary>The <c>KeyM</c> code - the key that prints <c>M</c> on a US layout.</summary>
    public const string KeyM = "KeyM";
    /// <summary>The <c>KeyN</c> code - the key that prints <c>N</c> on a US layout.</summary>
    public const string KeyN = "KeyN";
    /// <summary>The <c>KeyO</c> code - the key that prints <c>O</c> on a US layout.</summary>
    public const string KeyO = "KeyO";
    /// <summary>The <c>KeyP</c> code - the key that prints <c>P</c> on a US layout.</summary>
    public const string KeyP = "KeyP";
    /// <summary>The <c>KeyQ</c> code - the key that prints <c>Q</c> on a US layout.</summary>
    public const string KeyQ = "KeyQ";
    /// <summary>The <c>KeyR</c> code - the key that prints <c>R</c> on a US layout.</summary>
    public const string KeyR = "KeyR";
    /// <summary>The <c>KeyS</c> code - the key that prints <c>S</c> on a US layout.</summary>
    public const string KeyS = "KeyS";
    /// <summary>The <c>KeyT</c> code - the key that prints <c>T</c> on a US layout.</summary>
    public const string KeyT = "KeyT";
    /// <summary>The <c>KeyU</c> code - the key that prints <c>U</c> on a US layout.</summary>
    public const string KeyU = "KeyU";
    /// <summary>The <c>KeyV</c> code - the key that prints <c>V</c> on a US layout.</summary>
    public const string KeyV = "KeyV";
    /// <summary>The <c>KeyW</c> code - the key that prints <c>W</c> on a US layout.</summary>
    public const string KeyW = "KeyW";
    /// <summary>The <c>KeyX</c> code - the key that prints <c>X</c> on a US layout.</summary>
    public const string KeyX = "KeyX";
    /// <summary>The <c>KeyY</c> code - the key that prints <c>Y</c> on a US layout.</summary>
    public const string KeyY = "KeyY";
    /// <summary>The <c>KeyZ</c> code - the key that prints <c>Z</c> on a US layout.</summary>
    public const string KeyZ = "KeyZ";

    // Digits
    /// <summary>The <c>Digit0</c> code - <c>0</c> on the number row.</summary>
    public const string Digit0 = "Digit0";
    /// <summary>The <c>Digit1</c> code - <c>1</c> on the number row.</summary>
    public const string Digit1 = "Digit1";
    /// <summary>The <c>Digit2</c> code - <c>2</c> on the number row.</summary>
    public const string Digit2 = "Digit2";
    /// <summary>The <c>Digit3</c> code - <c>3</c> on the number row.</summary>
    public const string Digit3 = "Digit3";
    /// <summary>The <c>Digit4</c> code - <c>4</c> on the number row.</summary>
    public const string Digit4 = "Digit4";
    /// <summary>The <c>Digit5</c> code - <c>5</c> on the number row.</summary>
    public const string Digit5 = "Digit5";
    /// <summary>The <c>Digit6</c> code - <c>6</c> on the number row.</summary>
    public const string Digit6 = "Digit6";
    /// <summary>The <c>Digit7</c> code - <c>7</c> on the number row.</summary>
    public const string Digit7 = "Digit7";
    /// <summary>The <c>Digit8</c> code - <c>8</c> on the number row.</summary>
    public const string Digit8 = "Digit8";
    /// <summary>The <c>Digit9</c> code - <c>9</c> on the number row.</summary>
    public const string Digit9 = "Digit9";

    /// <summary>The <c>Numpad0</c> code - <c>0</c> on the numeric keypad.</summary>
    public const string Numpad0 = "Numpad0";
    /// <summary>The <c>Numpad1</c> code - <c>1</c> on the numeric keypad.</summary>
    public const string Numpad1 = "Numpad1";
    /// <summary>The <c>Numpad2</c> code - <c>2</c> on the numeric keypad.</summary>
    public const string Numpad2 = "Numpad2";
    /// <summary>The <c>Numpad3</c> code - <c>3</c> on the numeric keypad.</summary>
    public const string Numpad3 = "Numpad3";
    /// <summary>The <c>Numpad4</c> code - <c>4</c> on the numeric keypad.</summary>
    public const string Numpad4 = "Numpad4";
    /// <summary>The <c>Numpad5</c> code - <c>5</c> on the numeric keypad.</summary>
    public const string Numpad5 = "Numpad5";
    /// <summary>The <c>Numpad6</c> code - <c>6</c> on the numeric keypad.</summary>
    public const string Numpad6 = "Numpad6";
    /// <summary>The <c>Numpad7</c> code - <c>7</c> on the numeric keypad.</summary>
    public const string Numpad7 = "Numpad7";
    /// <summary>The <c>Numpad8</c> code - <c>8</c> on the numeric keypad.</summary>
    public const string Numpad8 = "Numpad8";
    /// <summary>The <c>Numpad9</c> code - <c>9</c> on the numeric keypad.</summary>
    public const string Numpad9 = "Numpad9";
    /// <summary>The <c>NumLock</c> code.</summary>
    public const string NumLock = "NumLock";
    /// <summary>The <c>NumpadAdd</c> code.</summary>
    public const string NumpadAdd = "NumpadAdd";
    /// <summary>The <c>NumpadMultiply</c> code.</summary>
    public const string NumpadMultiply = "NumpadMultiply";
    /// <summary>The <c>NumpadSubtract</c> code.</summary>
    public const string NumpadSubtract = "NumpadSubtract";
    /// <summary>The <c>NumpadDecimal</c> code.</summary>
    public const string NumpadDecimal = "NumpadDecimal";
    /// <summary>The <c>NumpadDivide</c> code.</summary>
    public const string NumpadDivide = "NumpadDivide";
    /// <summary>The <c>NumpadEnter</c> code.</summary>
    public const string NumpadEnter = "NumpadEnter";
    /// <summary>The <c>NumpadEqual</c> code.</summary>
    public const string NumpadEqual = "NumpadEqual";
    /// <summary>The <c>NumpadComma</c> code.</summary>
    public const string NumpadComma = "NumpadComma";

    // Function keys
    /// <summary>The <c>F1</c> code.</summary>
    public const string F1 = "F1";
    /// <summary>The <c>F2</c> code.</summary>
    public const string F2 = "F2";
    /// <summary>The <c>F3</c> code.</summary>
    public const string F3 = "F3";
    /// <summary>The <c>F4</c> code.</summary>
    public const string F4 = "F4";
    /// <summary>The <c>F5</c> code.</summary>
    public const string F5 = "F5";
    /// <summary>The <c>F6</c> code.</summary>
    public const string F6 = "F6";
    /// <summary>The <c>F7</c> code.</summary>
    public const string F7 = "F7";
    /// <summary>The <c>F8</c> code.</summary>
    public const string F8 = "F8";
    /// <summary>The <c>F9</c> code.</summary>
    public const string F9 = "F9";
    /// <summary>The <c>F10</c> code.</summary>
    public const string F10 = "F10";
    /// <summary>The <c>F11</c> code.</summary>
    public const string F11 = "F11";
    /// <summary>The <c>F12</c> code.</summary>
    public const string F12 = "F12";

    /// <summary>The <c>Backspace</c> code.</summary>
    public const string Backspace = "Backspace";
    /// <summary>The <c>Tab</c> code.</summary>
    public const string Tab = "Tab";
    /// <summary>The <c>Enter</c> code.</summary>
    public const string Enter = "Enter";
    /// <summary>The <c>ShiftLeft</c> code.</summary>
    public const string ShiftLeft = "ShiftLeft";
    /// <summary>The <c>ShiftRight</c> code.</summary>
    public const string ShiftRight = "ShiftRight";
    /// <summary>The <c>ControlLeft</c> code.</summary>
    public const string ControlLeft = "ControlLeft";
    /// <summary>The <c>ControlRight</c> code.</summary>
    public const string ControlRight = "ControlRight";
    /// <summary>The <c>AltLeft</c> code.</summary>
    public const string AltLeft = "AltLeft";
    /// <summary>The <c>AltRight</c> code.</summary>
    public const string AltRight = "AltRight";
    /// <summary>The <c>Pause</c> code.</summary>
    public const string PauseBreak = "Pause";
    /// <summary>The <c>CapsLock</c> code.</summary>
    public const string CapsLock = "CapsLock";
    /// <summary>The <c>Escape</c> code.</summary>
    public const string Escape = "Escape";
    /// <summary>The <c>Space</c> code.</summary>
    public const string Space = "Space";
    /// <summary>The <c>PageUp</c> code.</summary>
    public const string PageUp = "PageUp";
    /// <summary>The <c>PageDown</c> code.</summary>
    public const string PageDown = "PageDown";
    /// <summary>The <c>End</c> code.</summary>
    public const string End = "End";
    /// <summary>The <c>Home</c> code.</summary>
    public const string Home = "Home";
    /// <summary>The <c>ArrowLeft</c> code.</summary>
    public const string ArrowLeft = "ArrowLeft";
    /// <summary>The <c>ArrowUp</c> code.</summary>
    public const string ArrowUp = "ArrowUp";
    /// <summary>The <c>ArrowRight</c> code.</summary>
    public const string ArrowRight = "ArrowRight";
    /// <summary>The <c>ArrowDown</c> code.</summary>
    public const string ArrowDown = "ArrowDown";
    /// <summary>The <c>PrintScreen</c> code.</summary>
    public const string PrintScreen = "PrintScreen";
    /// <summary>The <c>Insert</c> code.</summary>
    public const string Insert = "Insert";
    /// <summary>The <c>Delete</c> code.</summary>
    public const string Delete = "Delete";
    /// <summary>The <c>MetaLeft</c> code.</summary>
    public const string MetaLeft = "MetaLeft";
    /// <summary>The <c>MetaRight</c> code.</summary>
    public const string MetaRight = "MetaRight";
    /// <summary>The <c>ContextMenu</c> code.</summary>
    public const string ContextMenu = "ContextMenu";
    /// <summary>The <c>ScrollLock</c> code.</summary>
    public const string ScrollLock = "ScrollLock";

    // Symbols
    /// <summary>The <c>Semicolon</c> code.</summary>
    public const string Semicolon = "Semicolon";
    /// <summary>The <c>Equal</c> code.</summary>
    public const string Equal = "Equal";
    /// <summary>The <c>Comma</c> code.</summary>
    public const string Comma = "Comma";
    /// <summary>The <c>Minus</c> code.</summary>
    public const string Minus = "Minus";
    /// <summary>The <c>Period</c> code.</summary>
    public const string Period = "Period";
    /// <summary>The <c>Slash</c> code.</summary>
    public const string Slash = "Slash";
    /// <summary>The <c>Backquote</c> code.</summary>
    public const string Backquote = "Backquote";
    /// <summary>The <c>BracketLeft</c> code.</summary>
    public const string BracketLeft = "BracketLeft";
    /// <summary>The <c>Backslash</c> code.</summary>
    public const string Backslash = "Backslash";
    /// <summary>The <c>BracketRight</c> code.</summary>
    public const string BracketRight = "BracketRight";
    /// <summary>The <c>Quote</c> code.</summary>
    public const string Quote = "Quote";
}
