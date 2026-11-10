using System.Security.Cryptography.X509Certificates;
using Bit.Cli.Infrastructure;
using Bit.Cli.Keys;
using Bit.Cli.Pipelines;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class PipelineTests
{
    [TestMethod]
    public async Task TheTemplatesWorkflows_Should_NeedTheirSecretsInEachEnvironment()
    {
        var scan = await new GitHubWorkflows(TemplateDirectory()).ScanAsync(CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { "Production", "Test" }, scan.Environments.ToArray(), $"{string.Join(", ", scan.Secrets.Select(s => $"{s.Environment}:{s.Name}"))} {string.Join(" | ", scan.Notes)}");

        foreach (var environment in scan.Environments)
        {
            var names = scan.Secrets.Where(s => s.Environment == environment).Select(s => s.Name).ToList();

            foreach (var name in new[] { "PUBLIC_VAPIDKEY", "PRIVATE_VAPIDKEY", "VAPID_SUBJECT", "APP_CERTIFICATE", "APP_CERTIFICATE_RETIRED", "SMTP_CONNECTION_STRING", "DATABASE_PASSWORD", "REDIS_CACHE_PASSWORD", "CLOUDFLARE_TUNNEL_TOKEN", "ANDROID_RELEASE_KEYSTORE_FILE_BASE64", "APPSTORE_API_KEY_ID" })
            {
                CollectionAssert.Contains(names, name, $"{environment}: {string.Join(", ", names)}");
            }
        }

        Assert.IsFalse(scan.Secrets.Any(s => s.Name is "GITHUB_TOKEN"));
        Assert.IsEmpty(scan.Notes, string.Join(Environment.NewLine, scan.Notes));
    }

    [TestMethod]
    public async Task AReusableWorkflow_Should_GetItsEnvironmentAndSecretsFromItsCaller()
    {
        using var host = new TestHost();
        Workflow(host, "deploy.yml", """
            on:
              push:
              workflow_dispatch:
                inputs:
                  target:
                    default: Staging
            env:
              SHARED: ${{ secrets.WORKFLOW_WIDE }}
            jobs:
              inherited:
                uses: ./.github/workflows/steps.yml
                with:
                  ENV_NAME: Production
                secrets: inherit
              mapped:
                uses: ./.github/workflows/steps.yml
                with:
                  ENV_NAME: ${{ inputs.target }}
                secrets:
                  TOKEN: ${{ secrets.CALLER_TOKEN }}
              remote:
                uses: contoso/pipelines/.github/workflows/shared.yml@v1
                secrets: inherit
              direct:
                runs-on: ubuntu-latest
                environment:
                  name: ${{ github.ref_name }}
                steps:
                  - run: echo ${{ secrets['INDEXED'] }} ${{ secrets.GITHUB_TOKEN }}
            """);
        Workflow(host, "steps.yml", """
            on:
              workflow_call:
                inputs:
                  ENV_NAME:
                    type: string
            jobs:
              deploy:
                runs-on: ubuntu-latest
                environment: ${{ inputs.ENV_NAME }}
                steps:
                  - run: echo ${{ secrets.TOKEN }}
              notify:
                runs-on: ubuntu-latest
                steps:
                  - run: echo ${{ secrets.TOKEN }}
            """);
        Workflow(host, "broken.yml", "on: [push\njobs: {");

        var fetched = new List<string>();
        var scan = await new GitHubWorkflows(host.WorkingDirectory, (uses, _) =>
        {
            fetched.Add(uses);
            return Task.FromResult<string?>("on:\n  workflow_call:\njobs:\n  remote:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.REMOTE_ONLY }}\n");
        }).ScanAsync(CancellationToken.None);

        var found = scan.Secrets.Select(s => $"{s.Environment ?? "repository"}:{s.Name}").ToList();

        CollectionAssert.AreEquivalent(new[]
        {
            "Production:TOKEN", "Production:REMOTE_ONLY", "Staging:TOKEN",
            "repository:TOKEN", "repository:CALLER_TOKEN", "repository:INDEXED", "repository:WORKFLOW_WIDE"
        }, found, string.Join(", ", found));

        CollectionAssert.AreEqual(new[] { "contoso/pipelines/.github/workflows/shared.yml@v1" }, fetched);
        Assert.IsTrue(scan.Notes.Any(n => n.Contains("broken.yml", StringComparison.Ordinal)), string.Join(Environment.NewLine, scan.Notes));
        Assert.IsTrue(scan.Notes.Any(n => n.Contains("github.ref_name", StringComparison.Ordinal)), string.Join(Environment.NewLine, scan.Notes));
    }

    [TestMethod]
    public async Task Pipeline_Should_CreateTheEnvironmentAndStoreTheKeysItCreates()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", """
            on: push
            jobs:
              deploy:
                runs-on: ubuntu-latest
                environment: Production
                steps:
                  - run: dotnet publish -p:AndroidSigningKeyAlias=Contoso -p:AndroidSigningKeyPass="${{ secrets.ANDROID_RELEASE_KEYSTORE_PASSWORD }}" -p:AndroidSigningStorePass="${{ secrets.ANDROID_RELEASE_SIGNING_PASSWORD }}"
                    env:
                      KEYSTORE: ${{ secrets.ANDROID_RELEASE_KEYSTORE_FILE_BASE64 }}
                      PUBLIC: ${{ secrets.PUBLIC_VAPIDKEY }}
                      PRIVATE: ${{ secrets.PRIVATE_VAPIDKEY }}
                      CERTIFICATE: ${{ secrets.APP_CERTIFICATE }}
                      RETIRED: ${{ secrets.APP_CERTIFICATE_RETIRED }}
                      DATABASE: ${{ secrets.DATABASE_CONNECTION_STRING }}
                      ALREADY: ${{ secrets.SHARED_SECRET }}
            """);

        host.Runner.On("gh", "secret list --repo contoso/shop --json", 0, "SHARED_SECRET\n");
        host.Runner.On("gh", "api repos/contoso/shop/environments/Production --silent", 1, "HTTP 404");

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--yes"));

        Assert.IsTrue(host.Runner.Calls.Any(c => string.Join(' ', c.Arguments) == "api -X PUT repos/contoso/shop/environments/Production --silent"), host.Output);

        var stored = Stored(host);
        CollectionAssert.AreEquivalent(new[] { "PUBLIC_VAPIDKEY", "PRIVATE_VAPIDKEY", "APP_CERTIFICATE", "ANDROID_RELEASE_KEYSTORE_FILE_BASE64", "ANDROID_RELEASE_KEYSTORE_PASSWORD", "ANDROID_RELEASE_SIGNING_PASSWORD" }, stored.Keys.ToArray(), host.Output);

        using var certificate = X509Certificate2.CreateFromPem(stored["APP_CERTIFICATE"], stored["APP_CERTIFICATE"]);
        StringAssert.Contains(certificate.Subject, "OU=Production");
        Assert.IsTrue(certificate.HasPrivateKey);

        Assert.AreEqual(stored["ANDROID_RELEASE_KEYSTORE_PASSWORD"], stored["ANDROID_RELEASE_SIGNING_PASSWORD"]);
        using var key = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(stored["ANDROID_RELEASE_KEYSTORE_FILE_BASE64"]), stored["ANDROID_RELEASE_KEYSTORE_PASSWORD"]);
        Assert.IsTrue(key.HasPrivateKey);
        Assert.AreEqual("CN=Contoso", key.Subject);

        Assert.IsTrue(host.Runner.Calls.All(c => stored.Values.All(v => c.Arguments.Any(a => a.Contains(v, StringComparison.Ordinal)) is false)), "a secret was passed on a command line");
        Assert.IsTrue(File.Exists(Path.Combine(host.Home, ".bitplatform", "keys", "contoso-shop", "Contoso.keystore")));
        Assert.IsTrue(File.Exists(Path.Combine(host.Home, ".bitplatform", "keys", "contoso-shop-Production", "AppCertificate.key")));
        StringAssert.Contains(host.FlatOutput, "Skipped DATABASE_CONNECTION_STRING");
    }

    [TestMethod]
    public async Task Pipeline_Should_CreateTheContainerPasswordsInLettersAndDigits()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.DATABASE_PASSWORD }} ${{ secrets.REDIS_CACHE_PASSWORD }} ${{ secrets.REDIS_PERSISTENT_PASSWORD }} ${{ secrets.SERVERWEB_SETTINGS }} ${{ secrets.LANDINGPAGE_SETTINGS }}\n");
        host.Runner.On("gh", "api repos/contoso/shop/environments/Production --silent", 0, "");

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--yes"));

        var stored = Stored(host);
        CollectionAssert.AreEquivalent(new[] { "DATABASE_PASSWORD", "REDIS_CACHE_PASSWORD", "REDIS_PERSISTENT_PASSWORD" }, stored.Keys.ToArray(), host.Output);
        Assert.IsTrue(stored.Values.All(password => password.Length == 32 && password.All(char.IsAsciiLetterOrDigit)), string.Join(", ", stored.Values));
        Assert.AreEqual(3, stored.Values.Distinct().Count());
        Assert.IsFalse(host.FlatOutput.Contains("Skipped SERVERWEB_SETTINGS", StringComparison.Ordinal), host.Output);
        Assert.IsFalse(host.FlatOutput.Contains("Skipped LANDINGPAGE_SETTINGS", StringComparison.Ordinal), host.Output);
    }

    [TestMethod]
    public async Task Check_Should_ListWhatIsMissingAndChangeNothing()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.APP_CERTIFICATE }} ${{ secrets.APP_CERTIFICATE_RETIRED }} ${{ secrets.DATABASE_CONNECTION_STRING }}\n");
        host.Runner.On("gh", "secret list --repo contoso/shop --env Production", 0, "APP_CERTIFICATE\n");

        Assert.AreEqual(CliApp.ExitCheckFailed, await host.RunAsync("pipeline", "--check"));

        Assert.IsEmpty(Stored(host));
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.Contains("PUT")));
        StringAssert.Matches(host.FlatOutput, new System.Text.RegularExpressions.Regex("DATABASE_CONNECTION_STRING missing"));
        StringAssert.Matches(host.FlatOutput, new System.Text.RegularExpressions.Regex("APP_CERTIFICATE_RETIRED not set, optional"));
    }

    [TestMethod]
    public async Task Check_Should_TreatAnyServicesSettingsAsOptional()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.SERVEROPERATIONS_SETTINGS }} ${{ secrets.SERVERMANAGEMENT_SETTINGS }}\n");
        host.Runner.On("gh", "secret list --repo contoso/shop --env Production", 0, "");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("pipeline", "--check"));

        StringAssert.Matches(host.FlatOutput, new System.Text.RegularExpressions.Regex("SERVEROPERATIONS_SETTINGS not set, optional"));
        StringAssert.Matches(host.FlatOutput, new System.Text.RegularExpressions.Regex("SERVERMANAGEMENT_SETTINGS not set, optional"));
    }

    [TestMethod]
    public async Task Pipeline_Should_NotAskForAnyServicesSettings()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.SERVEROPERATIONS_SETTINGS }} ${{ secrets.DATABASE_PASSWORD }}\n");
        host.Runner.On("gh", "api repos/contoso/shop/environments/Production --silent", 0, "");

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--yes"));

        CollectionAssert.AreEquivalent(new[] { "DATABASE_PASSWORD" }, Stored(host).Keys.ToArray(), host.Output);
        Assert.IsFalse(host.FlatOutput.Contains("Skipped SERVEROPERATIONS_SETTINGS", StringComparison.Ordinal), host.Output);
    }

    [TestMethod]
    public async Task SecretNames_Should_MatchWhateverTheirCase()
    {
        using var host = Repository();
        Workflow(host, "ci.yml", "on: push\njobs:\n  build:\n    runs-on: ubuntu-latest\n    steps:\n      - run: echo ${{ secrets.OpenAI__ApiKey }} ${{ secrets.OPENAI__APIKEY }}\n");
        host.Runner.On("gh", "secret list --repo contoso/shop --json", 0, "OPENAI__APIKEY\n");

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--check"));
        Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(host.Output, "OPENAI__APIKEY", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count, host.Output);
    }

    [TestMethod]
    public async Task RotatingTheCertificate_Should_KeepTheOldOneAsTheRetiredOne()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.APP_CERTIFICATE }} ${{ secrets.APP_CERTIFICATE_RETIRED }}\n");
        host.Runner.On("gh", "secret list --repo contoso/shop --env Production", 0, "APP_CERTIFICATE\n");

        var backup = Directory.CreateDirectory(Path.Combine(host.Home, ".bitplatform", "keys", "contoso-shop-Production")).FullName;
        var old = AppCertificates.CreateProduction();
        AppCertificates.Write(backup, AppCertificates.ActiveName, old);

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--rotate-certificate", "--yes"));

        var stored = Stored(host);
        Assert.AreEqual(old.Pem, stored["APP_CERTIFICATE_RETIRED"]);
        Assert.AreNotEqual(old.Pem, stored["APP_CERTIFICATE"]);
        Assert.AreEqual(stored["APP_CERTIFICATE"], AppCertificates.Read(backup, AppCertificates.ActiveName)!.Pem);
        Assert.HasCount(1, Directory.GetFiles(backup, "AppCertificate.*.crt"));
    }

    [TestMethod]
    public async Task ThePipeline_Should_CreateTheAppStoreSigningOnceForEveryEnvironment()
    {
        using var apple = new KeysTests.FakeAppStoreConnect();
        var prompter = new ScriptedPrompter();
        using var host = Repository(prompter);
        var p8 = Path.Combine(host.Root, "AuthKey_2X9R4HXF34.p8");
        File.WriteAllText(p8, apple.Key.PrivateKey);
        prompter.On("Its issuer id", apple.Key.IssuerId).On("Its key id", apple.Key.KeyId).On("Its .p8 file", p8);

        Workflow(host, "cd.yml", """
            on: push
            jobs:
              production:
                runs-on: macos-latest
                environment: Production
                steps:
                  - run: echo ${{ secrets.APPSTORE_API_KEY_ISSUER_ID }} ${{ secrets.APPSTORE_API_KEY_ID }} ${{ secrets.APPSTORE_API_KEY_PRIVATE_KEY }} ${{ secrets.APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_BASE64 }} ${{ secrets.APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_PASSWORD }}
              test:
                runs-on: macos-latest
                environment: Test
                steps:
                  - run: echo ${{ secrets.APPSTORE_API_KEY_ISSUER_ID }} ${{ secrets.APPSTORE_API_KEY_ID }} ${{ secrets.APPSTORE_API_KEY_PRIVATE_KEY }} ${{ secrets.APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_BASE64 }} ${{ secrets.APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_PASSWORD }}
            """);
        host.Runner.On("gh", "variable get APP_ID", 0, "com.contoso.shop\n");
        host.Runner.On("gh", "variable get IOS_CODE_SIGN_PROVISION", 1, "variable IOS_CODE_SIGN_PROVISION was not found");

        Assert.AreEqual(0, await new PipelineSetup(host.Services, new PipelineOptions([], false, false, false), apple).RunAsync(CancellationToken.None), host.Output);

        var sets = host.Runner.Calls.Where(c => c.Arguments is ["secret", "set", ..]).ToList();
        Assert.HasCount(10, sets, host.Output);
        Assert.AreEqual(1, apple.Requests.Count(r => r.Line is "POST /v1/certificates"));

        foreach (var environment in new[] { "Production", "Test" })
        {
            var stored = sets.Where(c => c.Arguments.Contains(environment)).ToDictionary(c => c.Arguments[2], c => c.StandardInput!);
            Assert.AreEqual(apple.Key.PrivateKey, stored["APPSTORE_API_KEY_PRIVATE_KEY"]);

            using var certificate = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(stored["APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_BASE64"]), stored["APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_PASSWORD"]);
            Assert.IsTrue(certificate.HasPrivateKey);
        }

        StringAssert.Contains(host.FlatOutput, "com.contoso.shop App Store");
        Assert.HasCount(1, Directory.GetFiles(Path.Combine(host.Home, ".bitplatform", "keys", "contoso-shop"), "com.contoso.shop.*.p12"));
    }

    [TestMethod]
    public async Task WithoutATerminal_Pipeline_Should_NeedYesBeforeItChangesAnything()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.APP_CERTIFICATE }}\n");
        host.Runner.On("gh", "api repos/contoso/shop/environments/Production --silent", 1, "HTTP 404");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("pipeline"));

        StringAssert.Contains(host.FlatOutput, "--yes");
        Assert.IsEmpty(Stored(host));
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.Contains("PUT")));
    }

    [TestMethod]
    public async Task ASecretThatCouldNotBeStored_Should_SayWhatBitRanButNotItsValue()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.PUBLIC_VAPIDKEY }} ${{ secrets.PRIVATE_VAPIDKEY }}\n");
        host.Runner.On("gh", "secret set", 1, "HTTP 403: Resource not accessible by integration");

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("pipeline", "--yes"));

        StringAssert.Contains(host.Output, $"bit ran this in {host.WorkingDirectory}, answering its prompts on standard input: gh secret set PUBLIC_VAPIDKEY --repo contoso/shop --env Production");
        Assert.IsTrue(Stored(host).Values.All(v => host.Output.Contains(v, StringComparison.Ordinal) is false), host.Output);
    }

    [TestMethod]
    public async Task Pipeline_Should_LockDownAPrivateRepositorysPullRequestsAndToken()
    {
        using var host = Repository();
        Workflow(host, "ci.yml", "on: push\njobs:\n  build:\n    runs-on: ubuntu-latest\n    steps:\n      - run: echo hi\n");
        host.Runner.On("gh", "api repos/contoso/shop --jq", 0, "private\nall\n");
        host.Runner.On("gh", "api repos/contoso/shop/actions/permissions/workflow --jq", 0, "write\ntrue\n");

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--yes"), host.Output);

        var commands = host.Runner.Calls.Select(c => string.Join(' ', c.Arguments)).ToList();
        CollectionAssert.Contains(commands, "api repos/contoso/shop -X PATCH -f pull_request_creation_policy=collaborators_only --silent", host.Output);
        CollectionAssert.Contains(commands, "api -X PUT repos/contoso/shop/actions/permissions/workflow -f default_workflow_permissions=read -F can_approve_pull_request_reviews=false --silent", host.Output);
        Assert.IsFalse(commands.Any(c => c.Contains("fork-pr-contributor-approval", StringComparison.Ordinal)), host.Output);
        StringAssert.Contains(host.FlatOutput, "Only collaborators can open pull requests");
    }

    [TestMethod]
    public async Task OnAPublicRepository_Pipeline_Should_RequireApprovalForOutsideContributorsAndWarnAboutTheRunner()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: [self-hosted, Production]\n    steps:\n      - run: echo hi\n");
        host.Runner.On("gh", "api repos/contoso/shop --jq", 0, "public\ncollaborators_only\n");
        host.Runner.On("gh", "api repos/contoso/shop/actions/permissions/workflow --jq", 0, "read\nfalse\n");
        host.Runner.On("gh", "api repos/contoso/shop/actions/permissions/fork-pr-contributor-approval --jq", 0, "first_time_contributors\n");

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--yes"), host.Output);

        var changes = host.Runner.Calls.Select(c => string.Join(' ', c.Arguments)).Where(c => c.Contains("PATCH", StringComparison.Ordinal) || c.Contains("PUT", StringComparison.Ordinal)).ToList();
        CollectionAssert.AreEqual(new[] { "api -X PUT repos/contoso/shop/actions/permissions/fork-pr-contributor-approval -f approval_policy=all_external_contributors --silent" }, changes, host.Output);
        StringAssert.Contains(host.FlatOutput, "keep the deploy jobs in a separate private repository");
    }

    [TestMethod]
    public async Task Check_Should_FailOnASettingThatIsOffAndChangeNothing()
    {
        using var host = Repository();
        Workflow(host, "ci.yml", "on: push\njobs:\n  build:\n    runs-on: ubuntu-latest\n    steps:\n      - run: echo hi\n");
        host.Runner.On("gh", "api repos/contoso/shop --jq", 0, "private\nall\n");
        host.Runner.On("gh", "api repos/contoso/shop/actions/permissions/workflow --jq", 0, "read\nfalse\n");

        Assert.AreEqual(CliApp.ExitCheckFailed, await host.RunAsync("pipeline", "--check"));

        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.Contains("PATCH") || c.Arguments.Contains("PUT")));
        StringAssert.Matches(host.FlatOutput, new System.Text.RegularExpressions.Regex("Pull requests from collaborators only off"));
        StringAssert.Matches(host.FlatOutput, new System.Text.RegularExpressions.Regex("Read-only GITHUB_TOKEN that can't approve pull requests on"));

        host.Runner.On("gh", "api repos/contoso/shop --jq", 0, "private\ncollaborators_only\n");
        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("pipeline", "--check"));
    }

    [TestMethod]
    public async Task Check_Should_FailWhenItCannotReadTheSettings()
    {
        using var host = Repository();
        Workflow(host, "ci.yml", "on: push\njobs:\n  build:\n    runs-on: ubuntu-latest\n    steps:\n      - run: echo hi\n");
        host.Runner.On("gh", "api repos/contoso/shop/actions/permissions/workflow --jq", 1, "HTTP 403");

        Assert.AreEqual(CliApp.ExitCheckFailed, await host.RunAsync("pipeline", "--check"));
        StringAssert.Contains(host.FlatOutput, "Couldn't read the pull request and Actions settings");
    }

    [TestMethod]
    public async Task Pipeline_Should_TurnOnDependabotAlertsAndLetProductionDeployFromMainOnly()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.DATABASE_PASSWORD }}\n");
        host.Runner.On("gh", "api repos/contoso/shop --jq", 0, "private\ncollaborators_only\n");
        host.Runner.On("gh", "api repos/contoso/shop/actions/permissions/workflow --jq", 0, "read\nfalse\n");
        host.Runner.On("gh", "api repos/contoso/shop/vulnerability-alerts --silent", 1, "gh: Vulnerability alerts are disabled. (HTTP 404)");
        host.Runner.On("gh", "api repos/contoso/shop/environments/Production", 0, """
            {"name":"Production","deployment_branch_policy":null,"protection_rules":[{"type":"wait_timer","wait_timer":5},{"type":"required_reviewers","prevent_self_review":true,"reviewers":[{"type":"User","reviewer":{"id":42,"login":"contoso"}}]}]}
            """);

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--yes"), host.Output);

        var changes = host.Runner.Calls.Where(c => c.Arguments.Contains("PUT") || c.Arguments.Contains("POST") || c.Arguments.Contains("PATCH")).ToList();
        CollectionAssert.AreEqual(new[]
        {
            "api -X PUT repos/contoso/shop/vulnerability-alerts --silent",
            "api -X PUT repos/contoso/shop/environments/Production --input - --silent",
            "api -X POST repos/contoso/shop/environments/Production/deployment-branch-policies -f name=main -f type=branch --silent"
        }, changes.Select(c => string.Join(' ', c.Arguments)).ToArray(), host.Output);

        var body = System.Text.Json.Nodes.JsonNode.Parse(changes[1].StandardInput!)!;
        Assert.IsTrue(body["deployment_branch_policy"]!["custom_branch_policies"]!.GetValue<bool>());
        Assert.AreEqual(5, body["wait_timer"]!.GetValue<int>());
        Assert.IsTrue(body["prevent_self_review"]!.GetValue<bool>());
        Assert.AreEqual(42, body["reviewers"]![0]!["id"]!.GetValue<int>());
        StringAssert.Contains(host.FlatOutput, "Production deploys from main only");
    }

    [TestMethod]
    public async Task Check_Should_SeeProductionsBranchRule()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.DATABASE_PASSWORD }}\n");
        host.Runner.On("gh", "api repos/contoso/shop --jq", 0, "private\ncollaborators_only\n");
        host.Runner.On("gh", "api repos/contoso/shop/actions/permissions/workflow --jq", 0, "read\nfalse\n");
        host.Runner.On("gh", "api repos/contoso/shop/environments/Production", 0, """{"name":"Production","deployment_branch_policy":{"protected_branches":false,"custom_branch_policies":true},"protection_rules":[]}""");
        host.Runner.On("gh", "secret list --repo contoso/shop --env Production", 0, "DATABASE_PASSWORD\n");
        host.Runner.On("gh", "api repos/contoso/shop/environments/Production/deployment-branch-policies", 0, "7 branch main\n");

        Assert.AreEqual(CliApp.ExitOk, await host.RunAsync("pipeline", "--check"), host.Output);
        StringAssert.Matches(host.FlatOutput, new System.Text.RegularExpressions.Regex("Production deploys from main only on"));

        host.Runner.On("gh", "api repos/contoso/shop/environments/Production/deployment-branch-policies", 0, "7 branch main\n8 branch develop\n");
        Assert.AreEqual(CliApp.ExitCheckFailed, await host.RunAsync("pipeline", "--check"), host.Output);
    }

    [TestMethod]
    public async Task Pipeline_Should_LeaveMainAsProductionsOnlyBranch()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.DATABASE_PASSWORD }}\n");
        host.Runner.On("gh", "api repos/contoso/shop --jq", 0, "private\ncollaborators_only\n");
        host.Runner.On("gh", "api repos/contoso/shop/actions/permissions/workflow --jq", 0, "read\nfalse\n");
        host.Runner.On("gh", "api repos/contoso/shop/environments/Production", 0, """{"name":"Production","deployment_branch_policy":{"protected_branches":false,"custom_branch_policies":true},"protection_rules":[]}""");
        host.Runner.On("gh", "secret list --repo contoso/shop --env Production", 0, "DATABASE_PASSWORD\n");
        host.Runner.On("gh", "api repos/contoso/shop/environments/Production/deployment-branch-policies", 0, "8 branch develop\n9 tag v*\n");

        Assert.AreEqual(0, await host.RunAsync("pipeline", "--yes"), host.Output);

        var changes = host.Runner.Calls.Select(c => string.Join(' ', c.Arguments)).Where(c => c.Contains("-X", StringComparison.Ordinal)).ToArray();
        CollectionAssert.AreEqual(new[]
        {
            "api -X PUT repos/contoso/shop/environments/Production --input - --silent",
            "api -X DELETE repos/contoso/shop/environments/Production/deployment-branch-policies/8 --silent",
            "api -X DELETE repos/contoso/shop/environments/Production/deployment-branch-policies/9 --silent",
            "api -X POST repos/contoso/shop/environments/Production/deployment-branch-policies -f name=main -f type=branch --silent"
        }, changes, host.Output);
    }

    [TestMethod]
    public async Task ProtectedBranchesOnly_Should_NotCountAsMainOnly()
    {
        using var host = Repository();
        Workflow(host, "cd.yml", "on: push\njobs:\n  deploy:\n    runs-on: ubuntu-latest\n    environment: Production\n    steps:\n      - run: echo ${{ secrets.DATABASE_PASSWORD }}\n");
        host.Runner.On("gh", "api repos/contoso/shop --jq", 0, "private\ncollaborators_only\n");
        host.Runner.On("gh", "api repos/contoso/shop/actions/permissions/workflow --jq", 0, "read\nfalse\n");
        host.Runner.On("gh", "api repos/contoso/shop/environments/Production", 0, """{"name":"Production","deployment_branch_policy":{"protected_branches":true,"custom_branch_policies":false},"protection_rules":[]}""");
        host.Runner.On("gh", "secret list --repo contoso/shop --env Production", 0, "DATABASE_PASSWORD\n");

        Assert.AreEqual(CliApp.ExitCheckFailed, await host.RunAsync("pipeline", "--check"), host.Output);
    }

    [TestMethod]
    public async Task WithoutATerminal_Pipeline_Should_NeedYesBeforeItChangesASetting()
    {
        using var host = Repository();
        Workflow(host, "ci.yml", "on: push\njobs:\n  build:\n    runs-on: ubuntu-latest\n    steps:\n      - run: echo hi\n");
        host.Runner.On("gh", "api repos/contoso/shop --jq", 0, "private\nall\n");
        host.Runner.On("gh", "api repos/contoso/shop/actions/permissions/workflow --jq", 0, "read\nfalse\n");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("pipeline"));

        StringAssert.Contains(host.FlatOutput, "--yes");
        Assert.IsFalse(host.Runner.Calls.Any(c => c.Arguments.Contains("PATCH") || c.Arguments.Contains("PUT")));
    }

    [TestMethod]
    public async Task Pipeline_Should_RefuseARepositoryWithoutGitHubWorkflows()
    {
        using var host = Repository();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("pipeline"));
        StringAssert.Contains(host.FlatOutput, "no GitHub Actions workflows");
    }

    private static TestHost Repository(IPrompter? prompter = null)
    {
        var host = new TestHost(prompter: prompter);
        host.Runner.Executables["gh"] = "gh";
        host.Runner.Executables["git"] = "git";
        host.Runner.On("git", "rev-parse --show-toplevel", 0, host.WorkingDirectory);
        host.Runner.On("gh", "repo view", 0, "contoso/shop\n");
        host.Runner.On("gh", "secret list", 0, "");
        return host;
    }

    private static void Workflow(TestHost host, string name, string content)
    {
        var folder = Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, ".github", "workflows")).FullName;
        File.WriteAllText(Path.Combine(folder, name), content);
    }

    private static Dictionary<string, string> Stored(TestHost host) => host.Runner.Calls
        .Where(c => c.Arguments is ["secret", "set", ..])
        .ToDictionary(c => c.Arguments[2], c => c.StandardInput!);

    private static string TemplateDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && Directory.Exists(Path.Combine(directory.FullName, "src", "Templates", "Boilerplate", "Bit.Boilerplate", ".github", "workflows")) is false)
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory);
        return Path.Combine(directory.FullName, "src", "Templates", "Boilerplate", "Bit.Boilerplate");
    }
}
