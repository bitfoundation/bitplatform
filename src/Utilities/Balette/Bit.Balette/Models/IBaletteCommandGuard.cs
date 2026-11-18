namespace Bit.Balette;

/// <summary>
/// Decides, every time a command is about to run, whether it may: the place for permission checks, feature flags and
/// tenancy rules that hold for many commands. Register one with <see cref="BitBalette.AddBitBaletteCommandGuard{TGuard}"/>;
/// it is resolved from the scope, so it can depend on scoped services such as an <c>AuthenticationStateProvider</c>.
/// </summary>
/// <remarks>
/// <para>
/// Guards run at execution time - after a row was chosen, a shortcut pressed or <see cref="IBaletteService.ExecuteAsync"/>
/// called, and again for the second <c>Enter</c> of a confirmation and when a prompt or multi-select page is submitted,
/// and before <see cref="IBaletteService.OpenAsync"/> opens a page by id - not while the list is built, which happens on
/// every keystroke. Hide what a user can never run with
/// <see cref="BaletteCommandBuilder.VisibleWhen"/>; enforce it with a guard.
/// </para>
/// <para>
/// A command's own guard and its ancestors' run first (outermost first), then every registered guard, and the first
/// refusal wins. A guard that throws refuses (it is logged). On WebAssembly a guard runs in the browser like the rest of
/// the app: it keeps the palette honest, and the server still has to check what a handler asks it to do.
/// </para>
/// </remarks>
public interface IBaletteCommandGuard
{
    /// <summary>Answers whether <see cref="BaletteGuardContext.Command"/> may run now.</summary>
    /// <param name="context">The command and what started it.</param>
    /// <param name="cancellationToken">Cancelled when the palette closes or is disposed while the guard is still deciding.</param>
    ValueTask<BaletteGuardResult> CheckAsync(BaletteGuardContext context, CancellationToken cancellationToken);
}
