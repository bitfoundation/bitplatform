using System.CommandLine;
using Bit.Cli.Keys;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class KeysCommand
{
    public static Command Create(Func<CliServices> services)
    {
        return new Command("keys", "Create the keys and certificates a bit Boilerplate project signs and encrypts with.")
        {
            Vapid(services)
        };
    }

    private static Command Vapid(Func<CliServices> services)
    {
        var command = new Command("vapid", "Create a VAPID key pair for web push notifications.");

        command.SetAction(_ =>
        {
            var cli = services();
            var keys = VapidKeys.Create();

            cli.Console.Out.MarkupLine($"  [grey]Public key[/]   {keys.PublicKey}");
            cli.Console.Out.MarkupLine($"  [grey]Private key[/]  {keys.PrivateKey}");
            cli.Console.Out.MarkupLine("[grey]The public key goes in AdsPushVapid:PublicKey of Client.Web, the private key in AdsPushVapid:PrivateKey of the server. Keep the private key out of the repository.[/]");
            return CliApp.ExitOk;
        });

        return command;
    }
}
