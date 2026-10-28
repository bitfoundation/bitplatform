using System.CommandLine;
using Bit.Cli.AotProfiling;

namespace Bit.Cli.Commands;

public static class AotProfileCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var projectArgument = new Argument<string?>("project")
        {
            Description = "The Blazor WebAssembly app's .csproj, or the server's that hosts it. Default: the .csproj in this folder.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var outputOption = new Option<string?>("--output", "-o") { Description = "Where to write the profile. Default: <project>.aotprofile in this folder." };
        var propertyOption = new Option<string[]>("--property", "-p") { Description = "An MSBuild property for the publish, e.g. -p:InvariantGlobalization=true. Pass the ones the AOT publish gets, so both trim and minify alike.", HelpName = "name=value" };
        var originOption = new Option<string?>("--origin") { Description = $"For an app without a server: the address it's served at, e.g. its real one, so its API accepts the requests. Default: {PublishedApp.DefaultOrigin.ToString().TrimEnd('/')}." };
        var pathOption = new Option<string>("--path") { Description = "The page to start on.", DefaultValueFactory = _ => "/" };
        var stepOption = new Option<string[]>("--step") { Description = "Something to do once every page has been visited: a path like /settings, click:<css selector>, key:<key> or wait:<milliseconds>. Repeat it for more." };
        var maxPagesOption = new Option<int>("--max-pages") { Description = "Visit at most this many pages, following the app's links.", DefaultValueFactory = _ => 50 };
        var includeOption = new Option<string[]>("--include") { Description = "An assembly whose every method gets compiled, run or not, e.g. --include Bit.Bmage. Repeat it for more." };
        var browserOption = new Option<string?>("--browser") { Description = "The Chromium, Chrome or Edge to use. Default: Playwright's Chromium, then Chrome, then Edge." };

        var command = new Command("aot-profile", "Publish a Blazor WebAssembly app on the interpreter with Mono's AOT profiler, use it in a headless browser, and write the .aotprofile that -p:WasmAotProfilePath compiles ahead of time.")
        {
            projectArgument,
            outputOption,
            propertyOption,
            originOption,
            pathOption,
            stepOption,
            maxPagesOption,
            includeOption,
            browserOption
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var cli = services();
            var (properties, error) = SharedOptions.ParseProperties(parseResult.GetValue(propertyOption));

            if (error is not null)
            {
                return Task.FromResult(cli.UsageError("properties-invalid", error));
            }

            var request = new AotProfileRequest(
                parseResult.GetValue(projectArgument),
                parseResult.GetValue(outputOption),
                properties,
                parseResult.GetValue(originOption),
                parseResult.GetValue(pathOption) ?? "/",
                parseResult.GetValue(stepOption) ?? [],
                parseResult.GetValue(maxPagesOption),
                SharedOptions.SplitList(parseResult.GetValue(includeOption)),
                parseResult.GetValue(browserOption));

            return new AotProfileRun(cli).RunAsync(request, cancellationToken);
        });

        return command;
    }
}
