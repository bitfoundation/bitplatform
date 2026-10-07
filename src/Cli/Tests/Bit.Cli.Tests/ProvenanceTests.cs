using System.Net;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class ProvenanceTests
{
    [TestMethod]
    [DataRow("""{"attestations":[]}""", 0)]
    [DataRow("""{"attestations":[{"repository_id":1},{"repository_id":1}]}""", 2)]
    [DataRow("""{"message":"Not Found"}""", 0)]
    [DataRow("[]", 0)]
    public void Attestations_Should_BeCounted(string json, int count)
    {
        Assert.AreEqual(count, Provenance.CountAttestations(json));
    }

    [TestMethod]
    [DataRow(HttpStatusCode.OK, """{"attestations":[{"repository_id":1}]}""", AttestationState.Found)]
    [DataRow(HttpStatusCode.OK, """{"attestations":[]}""", AttestationState.Missing)]
    [DataRow(HttpStatusCode.NotFound, """{"message":"Not Found"}""", AttestationState.Missing)]
    [DataRow(HttpStatusCode.Forbidden, """{"message":"API rate limit exceeded"}""", AttestationState.Unavailable)]
    [DataRow(HttpStatusCode.OK, "not json", AttestationState.Unavailable)]
    public async Task TheAttestationLookup_Should_AskGitHubForTheFilesDigest(HttpStatusCode status, string body, AttestationState expected)
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "bit");
            using var handler = new StubHandler(status, body);

            Assert.AreEqual(expected, await Provenance.FindAttestationAsync(handler, file, CancellationToken.None));
            Assert.AreEqual(Provenance.AttestationsApiUrl(Provenance.Sha256(file)), handler.Request!.RequestUri!.ToString());
            StringAssert.StartsWith(handler.Request.Headers.UserAgent.ToString(), "bit-cli/");
        }
        finally
        {
            File.Delete(file);
        }
    }

    [TestMethod]
    public async Task TheAttestationLookup_Should_SayGitHubIsUnavailable_When_TheRequestFails()
    {
        using var handler = new StubHandler(new HttpRequestException("offline"));

        Assert.AreEqual(AttestationState.Unavailable, await Provenance.FindAttestationAsync(handler, typeof(ProvenanceTests).Assembly.Location, CancellationToken.None));
    }

    [TestMethod]
    public void TheWindowsSignature_Should_TellSignedUnsignedAndChangedFilesApart()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        var signed = Provenance.WindowsSignature(typeof(object).Assembly.Location);
        Assert.AreEqual(SignatureState.Valid, signed.State);
        StringAssert.Contains(signed.Signer, ".NET");

        Assert.AreEqual(SignatureState.Unsigned, Provenance.WindowsSignature(typeof(ProvenanceTests).Assembly.Location).State);

        var changed = Path.Combine(Path.GetTempPath(), $"bit-signature-{Guid.NewGuid():N}.dll");
        try
        {
            var bytes = File.ReadAllBytes(typeof(object).Assembly.Location);
            bytes[bytes.Length / 2] ^= 0xFF;
            File.WriteAllBytes(changed, bytes);

            Assert.AreEqual(SignatureState.Invalid, Provenance.WindowsSignature(changed).State);
        }
        finally
        {
            File.Delete(changed);
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode status;
        private readonly string body = "";
        private readonly Exception? exception;

        public StubHandler(HttpStatusCode status, string body)
        {
            this.status = status;
            this.body = body;
        }

        public StubHandler(Exception exception)
        {
            this.exception = exception;
        }

        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            if (exception is not null)
                throw exception;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
