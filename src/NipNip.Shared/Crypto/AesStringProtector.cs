using System.Security.Cryptography;
using System.Text;

namespace NipNip.Shared.Crypto;

/// <summary>
/// AES-256-GCM string encryption keyed by a caller-provided base64-encoded 32-byte key.
/// Deliberately not tied to ASP.NET Core Data Protection: that API's key ring is
/// filesystem-based by default, which is unsafe on deployments without a confirmed
/// persistent volume (a redeploy could make every encrypted value permanently
/// undecryptable). A static config-provided key avoids that trap.
/// </summary>
public class AesStringProtector(string base64Key)
{
    private readonly byte[] _key = Convert.FromBase64String(base64Key);

    public string Encrypt(string plaintext)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var cipherBytes = new byte[plainBytes.Length];

        using (var aes = new AesGcm(_key, tag.Length))
        {
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        var combined = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string ciphertext)
    {
        var combined = Convert.FromBase64String(ciphertext);
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var nonce = combined[..nonceSize];
        var tag = combined[nonceSize..(nonceSize + tagSize)];
        var cipherBytes = combined[(nonceSize + tagSize)..];
        var plainBytes = new byte[cipherBytes.Length];

        using (var aes = new AesGcm(_key, tagSize))
        {
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}
