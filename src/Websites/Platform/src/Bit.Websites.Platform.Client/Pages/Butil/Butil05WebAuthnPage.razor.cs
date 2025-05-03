using System.Text.Json;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil05WebAuthnPage
{
    private object? createResult;
    private object? getResult;
    private bool? verifyResult;

    private async Task Create()
    {
        createResult = await webAuthn.CreateCredential(new
        {
            challenge = "testChallenge",
            rp = new { name = "testRp" },
            user = new { id = "userId", name = "testUser", displayName = "testUser" },
            pubKeyCredParams = new object[] { new { alg = -7, type = "public-key" } }
        });
    }

    private async Task Get()
    {
        getResult = await webAuthn.GetCredential(new { challenge = "test" });
    }

    private async Task Verify()
    {
        verifyResult = await webAuthn.Verify();
    }


    private string createExampleCode =
@"@inject Bit.Butil.WebAuthn webAuthn

<BitButton OnClick=""@Create"">Create</BitButton>

<div>Result:</div>
<div>@createResult?.ToString()?.Replace("","", "",\n"")</div>

@code {
    private object? createResult;

    private async Task Create()
    {
        createResult = await webAuthn.CreateCredential(new
        {
            challenge = ""testChallenge"",
            rp = new { name = ""testRp"" },
            user = new { id = ""userId"", name = ""testUser"", displayName = ""testUser"" },
            pubKeyCredParams = new object[] { new { alg = -7, type = ""public-key"" } }
        });
    }
}";

    private string getExampleCode =
@"@inject Bit.Butil.WebAuthn webAuthn

<BitButton OnClick=""@Get"">Get</BitButton>

<div>Result:</div>
<div>@getResult?.ToString()?.Replace("","", "",\n"")</div>

@code {
    private object? getResult;

    private async Task Get()
    {
        getResult = await webAuthn.GetCredential(new { challenge = ""test"" });
    }
}";

    private string verifyExampleCode =
@"@inject Bit.Butil.WebAuthn webAuthn

<BitButton OnClick=""@Verify"">Verify</BitButton>

<div>Result: @verifyResult</div>

@code {
    private bool? verifyResult;

    private async Task Verify()
    {
        verifyResult = await webAuthn.Verify();
    }
}";
}
