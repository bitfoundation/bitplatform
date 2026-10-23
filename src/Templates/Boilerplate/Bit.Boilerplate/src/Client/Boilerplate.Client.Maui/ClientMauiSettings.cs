//+:cnd:noEmit
using Boilerplate.Client.Core;
using Microsoft.Extensions.Options;

namespace Boilerplate.Client.Maui;

public class ClientMauiSettings : ClientCoreSettings
{

}

[OptionsValidator]
public partial class ClientMauiSettingsValidator : IValidateOptions<ClientMauiSettings>
{
}
