using Microsoft.AspNetCore.Components.Forms;

// Stands in for the framework's form-mapping subscription in the tests (FormsTests): what it is, is decided by the
// namespace it is declared in, so the stand-in has to live in that namespace too.
namespace Microsoft.AspNetCore.Components.Forms.Mapping;

internal static class FakeMappingSubscriptions
{
    public static void Attach(EditContext context) => context.OnValidationRequested += OnValidationRequested;

    private static void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
    }
}
