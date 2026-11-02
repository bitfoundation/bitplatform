namespace Bit.Borm;

/// <summary>How a form's <see cref="BormDefinition.Steps"/> are presented.</summary>
public enum BormStepLayout
{
    /// <summary>
    /// One step at a time, with Back and Next, each validated before the next is reached — for a
    /// process the user goes through in order: sign-up, checkout, an application.
    /// </summary>
    Wizard,

    /// <summary>
    /// Tabs over one form, any of them open at a time — for a long record the user moves around in
    /// freely: a settings page, a customer's details. Submit validates every tab, and a tab with
    /// something to fix says so.
    /// </summary>
    Tabs,

    /// <summary>
    /// Collapsible sections (the browser's own disclosure widget), the first open — a long form whose
    /// parts the user can fold away.
    /// </summary>
    Accordion,

    /// <summary>Titled sections one after another, all open — a long form that reads top to bottom.</summary>
    Sections
}
