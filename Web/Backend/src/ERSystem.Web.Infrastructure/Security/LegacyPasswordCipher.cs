using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ERSystem.Web.Infrastructure.Security;

public sealed class LegacyAuthenticationOptions
{
    public const string SectionName = "LegacyAuthentication";
    public string EncryptionKey { get; set; } = string.Empty;
}

public sealed class LegacyPasswordCipher(IOptions<LegacyAuthenticationOptions> options)
{
    private readonly string _key = string.IsNullOrWhiteSpace(options.Value.EncryptionKey)
        ? throw new InvalidOperationException("LegacyAuthentication:EncryptionKey must be configured.")
        : options.Value.EncryptionKey;

    public string Encrypt(string plainText)
    {
        using var tripleDes = TripleDES.Create();
        tripleDes.Key = TruncateHash(_key, tripleDes.KeySize / 8);
        tripleDes.IV = TruncateHash(string.Empty, tripleDes.BlockSize / 8);
        var bytes = Encoding.Unicode.GetBytes(plainText);
        var encrypted = tripleDes.CreateEncryptor().TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToHexString(Encoding.ASCII.GetBytes(Convert.ToBase64String(encrypted)));
    }

    private static byte[] TruncateHash(string value, int length)
    {
        var hash = SHA1.HashData(Encoding.Unicode.GetBytes(value));
        var result = new byte[length];
        Array.Copy(hash, result, Math.Min(hash.Length, result.Length));
        return result;
    }
}
