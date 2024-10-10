using Boilerplate.Server.Api.Services;

namespace Boilerplate.Tests.Services;

public partial class FakeGoogleRecaptchaHttpClient : GoogleRecaptchaHttpClient
{
    public FakeGoogleRecaptchaHttpClient() : base(null, null) { }

    public override ValueTask<bool> Verify(string? googleRecaptchaResponse, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(true);
    }
}