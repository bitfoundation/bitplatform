using Bit.Cli.Projects;

namespace Bit.Cli.Tests;

[TestClass]
public class RepositoryAddressTests
{
    [TestMethod]
    [DataRow("https://github.com/contoso/Contoso", "github.com", "contoso/Contoso", "Contoso", true)]
    [DataRow("https://github.com/contoso/Contoso.git", "github.com", "contoso/Contoso", "Contoso", true)]
    [DataRow("https://github.com/contoso/Contoso/", "github.com", "contoso/Contoso", "Contoso", true)]
    [DataRow("https://gitlab.com/contoso/apps/Contoso.Shop", "gitlab.com", "contoso/apps/Contoso.Shop", "Contoso.Shop", false)]
    [DataRow("https://dev.azure.com/contoso/Shop/_git/Contoso", "dev.azure.com", "contoso/Shop/_git/Contoso", "Contoso", false)]
    [DataRow("https://git.contoso.com:8443/team/Contoso", "git.contoso.com:8443", "team/Contoso", "Contoso", false)]
    [DataRow("git@github.com:contoso/Contoso.git", "github.com", "contoso/Contoso", "Contoso", false)]
    public void AnAddress_Should_GiveItsHostPathAndFolderName(string address, string host, string path, string name, bool signsInWithGitHubCli)
    {
        var repository = RepositoryAddress.Parse(address);

        Assert.IsNotNull(repository);
        Assert.IsTrue(RepositoryAddress.LooksLikeOne(address));
        Assert.AreEqual(host, repository.Host);
        Assert.AreEqual(path, repository.Path);
        Assert.AreEqual(name, repository.Name);
        Assert.AreEqual(signsInWithGitHubCli, repository.SignsInWithGitHubCli);
    }

    // Each of these goes into a line bitfabric.co gives for PowerShell or bash, or would name a folder outside the current one
    [TestMethod]
    [DataRow("https://github.com/contoso")]
    [DataRow("https://github.com/contoso/Contoso?tab=readme")]
    [DataRow("https://github.com/contoso/Contoso#readme")]
    [DataRow("https://github.com/contoso/Contoso;rm -rf ~")]
    [DataRow("https://github.com/contoso/$(whoami)")]
    [DataRow("https://github.com/contoso/Contoso\"")]
    [DataRow("https://github.com/contoso/..")]
    [DataRow("https://user@github.com/contoso/Contoso")]
    [DataRow("http://github.com/contoso/Contoso")]
    [DataRow("ssh://git@github.com/contoso/Contoso")]
    public void AnAddressBitCantCloneSafely_Should_BeRefused(string address)
    {
        Assert.IsTrue(RepositoryAddress.LooksLikeOne(address));
        Assert.IsNull(RepositoryAddress.Parse(address));
    }

    [TestMethod]
    [DataRow("Contoso")]
    [DataRow("./Contoso")]
    [DataRow("../work/Contoso")]
    [DataRow(@"C:\src\Contoso")]
    [DataRow("/home/me/src/Contoso")]
    public void AFolder_Should_NotBeTakenForAnAddress(string folder)
    {
        Assert.IsFalse(RepositoryAddress.LooksLikeOne(folder));
    }

    [TestMethod]
    [DataRow("https://github.com/contoso/Contoso.git")]
    [DataRow("https://github.com/Contoso/contoso/")]
    [DataRow("git@github.com:contoso/Contoso.git")]
    [DataRow("https://someone@github.com/contoso/Contoso")]
    public void ACloneOfTheSameRepository_Should_BeRecognizedWhateverTheFormOfItsOrigin(string origin)
    {
        Assert.IsTrue(RepositoryAddress.Parse("https://github.com/contoso/Contoso")!.IsSameAs(origin));
    }

    [TestMethod]
    [DataRow("https://github.com/contoso/Contoso.Shop")]
    [DataRow("https://gitlab.com/contoso/Contoso")]
    [DataRow("")]
    public void AnotherRepository_Should_NotBeTakenForTheSameOne(string origin)
    {
        Assert.IsFalse(RepositoryAddress.Parse("https://github.com/contoso/Contoso")!.IsSameAs(origin));
    }
}
