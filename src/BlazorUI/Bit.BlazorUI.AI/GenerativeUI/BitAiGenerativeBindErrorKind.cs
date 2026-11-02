namespace Bit.BlazorUI;

/// <summary>
/// Why a property of a generated component did not bind (<see cref="BitAiGenerativeBindError.Kind"/>).
/// </summary>
public enum BitAiGenerativeBindErrorKind
{
    /// <summary>The value the model wrote could not be read as the type the component declares for it.</summary>
    Unreadable = 0,

    /// <summary>A property the component requires was not supplied.</summary>
    Missing = 1,
}
