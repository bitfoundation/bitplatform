using System.CommandLine;
using Bit.Cli.Pipelines;
using Bit.Cli.Projects;

namespace Bit.Cli.Commands;

public static class PipelineCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var environmentOption = new Option<string[]>("--env")
        {
            Description = "Only these GitHub environments. Default: every environment the workflows deploy to.",
            AllowMultipleArgumentsPerToken = true,
            HelpName = "Production,Test"
        };

        var checkOption = new Option<bool>("--check") { Description = "Only list the secrets and the repository's settings, and exit with code 3 when one the workflows need is missing or a setting is off." };
        var yesOption = new Option<bool>("--yes", "-y") { Description = "Store the secrets bit creates and turn on the repository's settings without asking, and skip the secrets only you can give." };
        var rotateOption = new Option<bool>("--rotate-certificate") { Description = "Give each environment a new app certificate, keeping the old one as APP_CERTIFICATE_RETIRED so nobody is signed out." };
        var firebaseOption = new Option<string?>("--firebase-service-account") { Description = "Android push: the Firebase service account key (.json) the server sends with, stored as its six SERVERAPI__ADSPUSHFIREBASE__ settings.", HelpName = "file" };
        var googleServicesOption = new Option<string?>("--google-services-json") { Description = "Android push: the Android app's google-services.json, stored in base64 as GOOGLE_SERVICES_JSON_FILE_BASE64.", HelpName = "file" };
        var apnsOption = new Option<string?>("--apns-key") { Description = "iOS push: the APNs key's AuthKey_<key id>.p8, stored as the server's SERVERAPI__ADSPUSHAPNS__ settings.", HelpName = "file" };
        var signInWithAppleOption = new Option<string?>("--sign-in-with-apple-key") { Description = "Sign in with Apple: its key's AuthKey_<key id>.p8, stored with --apple-services-id as the server's SERVERAPI__AUTHENTICATION__APPLE__ settings.", HelpName = "file" };
        var servicesIdOption = new Option<string?>("--apple-services-id") { Description = "Sign in with Apple's Services ID, e.g. com.contoso.shop.signin." };
        var teamIdOption = new Option<string?>("--apple-team-id") { Description = "The Apple team id, for the APNs and Sign in with Apple keys. Default: the one of the App Store signing bit creates in the same run; otherwise bit asks." };

        var command = new Command("pipeline", "Fill in the secrets your GitHub Actions workflows use: bit reads them from the workflows, creates the environments they deploy to, creates the keys it can and asks for the rest. It also locks down the repository: only collaborators open pull requests, the workflows' token is read-only, Production deploys from main only and Dependabot alerts are on.")
        {
            environmentOption,
            checkOption,
            yesOption,
            rotateOption,
            firebaseOption,
            googleServicesOption,
            apnsOption,
            signInWithAppleOption,
            servicesIdOption,
            teamIdOption
        };

        var projectOptions = new ProjectOptions();
        projectOptions.Project.Description = "A folder of the project, in its git repository. Default: the repository's root, or else the nearest folder with a *.Web.slnf or *.slnx, the current one or one above it.";
        projectOptions.AddTo(command, projectOptions.Project, projectOptions.ServerApi, projectOptions.Maui, projectOptions.WellKnown);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();
            var folder = projectOptions.Folder(parseResult, cli);
            var projectDirectory = ProjectContext.FindProjectDirectory(folder);

            if (projectDirectory is null && projectOptions.IsGiven(parseResult))
                return cli.UsageError("not-a-project", $"{folder} isn't in a bit Boilerplate project: neither it nor a folder above it has a *.Web.slnf or *.slnx.");

            var (parts, error) = projectOptions.Read(parseResult, cli, projectDirectory ?? folder);

            if (error is not null)
                return cli.UsageError("project-part-missing", error);

            var options = new PipelineOptions(
                SharedOptions.SplitList(parseResult.GetValue(environmentOption)),
                parseResult.GetValue(checkOption),
                parseResult.GetValue(yesOption),
                parseResult.GetValue(rotateOption))
            {
                FirebaseServiceAccount = parseResult.GetValue(firebaseOption),
                GoogleServicesJson = parseResult.GetValue(googleServicesOption),
                ApnsKey = parseResult.GetValue(apnsOption),
                SignInWithAppleKey = parseResult.GetValue(signInWithAppleOption),
                AppleServicesId = parseResult.GetValue(servicesIdOption)?.Trim(),
                AppleTeamId = parseResult.GetValue(teamIdOption)?.Trim(),
                Project = projectOptions.IsGiven(parseResult) ? projectDirectory : null,
                Parts = parts
            };

            return await new PipelineSetup(cli, options).RunAsync(cancellationToken);
        });

        return command;
    }
}
