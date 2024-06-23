using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil04CryptoPage
{
    private byte[] iv = new byte[16];
    private byte[] key = new byte[16];

    private string? encryptText;
    private byte[]? encryptedBytes;
    private string? encryptedText;

    private string? decryptText;
    private byte[]? decryptedBytes;
    private string? decryptedText;


    protected override async Task OnInitAsync()
    {
        System.Security.Cryptography.RandomNumberGenerator.Fill(iv);
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
    }

    private async Task Encrypt()
    {
        var textAsUtf8Bytes = System.Text.Encoding.UTF8.GetBytes(encryptText!);
        encryptedBytes = await crypto.Encrypt(CryptoAlgorithm.AesCbc, key, textAsUtf8Bytes, iv: iv);
        encryptedText = BitConverter.ToString(encryptedBytes!);
    }

    private async Task Decrypt()
    {
        decryptedBytes = await crypto.Decrypt(CryptoAlgorithm.AesCbc, key, encryptedBytes!, iv: iv);
        decryptedText = System.Text.Encoding.UTF8.GetString(decryptedBytes);
    }


    private string encryptExampleCode =
@"@inject Bit.Butil.Crypto crypto

<BitTextField @bind-Value=""encryptText"" />

<BitButton OnClick=""@Encrypt"">Encrypt</BitButton>

<div>Encrypted text: @encryptedText</div>

@code {
    private byte[] iv = new byte[16];
    private byte[] key = new byte[16];

    private string? encryptText;
    private byte[]? encryptedBytes;
    private string? encryptedText;

    protected override async Task OnInitAsync()
    {
        System.Security.Cryptography.RandomNumberGenerator.Fill(iv);
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
    }

    private async Task Encrypt()
    {
        var textAsUtf8Bytes = System.Text.Encoding.UTF8.GetBytes(encryptText!);
        encryptedBytes = await crypto.Encrypt(CryptoAlgorithm.AesCbc, key, textAsUtf8Bytes, iv: iv);
        encryptedText = BitConverter.ToString(encryptedBytes!);
    }
}";
    private string decryptExampleCode =
@"@inject Bit.Butil.Crypto crypto

<BitTextField @bind-Value=""decryptText"" />

<BitButton OnClick=""@Decrypt"">Decrypt</BitButton>

<div>Decrypted text: @decryptedText</div>

@code {
    private byte[] iv = new byte[16];
    private byte[] key = new byte[16];

    private string? decryptText;
    private byte[]? decryptedBytes;
    private string? decryptedText;

    protected override async Task OnInitAsync()
    {
        System.Security.Cryptography.RandomNumberGenerator.Fill(iv);
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
    }

    private async Task Decrypt()
    {
        decryptedBytes = await crypto.Decrypt(CryptoAlgorithm.AesCbc, key, encryptedBytes!, iv: iv);
        decryptedText = System.Text.Encoding.UTF8.GetString(decryptedBytes);
    }
}";
}
