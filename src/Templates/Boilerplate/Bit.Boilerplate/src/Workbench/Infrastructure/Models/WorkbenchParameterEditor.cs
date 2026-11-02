namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>How the component panel edits a parameter; ReadOnly for the types it can't write, like templates and callbacks.</summary>
public enum WorkbenchParameterEditor
{
    ReadOnly,
    Toggle,
    Choice,
    Text,
    Number,
    Parts
}
