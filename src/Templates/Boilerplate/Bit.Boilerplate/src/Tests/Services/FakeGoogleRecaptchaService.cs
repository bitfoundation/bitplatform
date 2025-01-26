using Boilerplate.Server.Api.Services;

namespace Boilerplate.Tests.Services;

public partial class FakeGoogleRecaptchaService : GoogleRecaptchaService
{
    public FakeGoogleRecaptchaService() : base(null, null) { }

    public override ValueTask<bool> Verify(string? googleRecaptchaResponse, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(true);
    }
}
