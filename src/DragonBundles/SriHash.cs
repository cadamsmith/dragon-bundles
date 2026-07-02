using System.Security.Cryptography;
using System.Text;

namespace DragonBundles;

/// <summary>
/// Computes a <a href="https://developer.mozilla.org/en-US/docs/Web/Security/Subresource_Integrity">
/// Subresource Integrity</a> string (<c>sha384-&lt;base64&gt;</c>) over a bundle's served bytes.
/// TFM-neutral so both runtimes emit identical hashes: the hash must cover the exact bytes the
/// browser downloads, so callers pass the final post-minification content.
/// </summary>
static class SriHash
{
    public static string Compute(byte[] content)
    {
        // The static SHA384.HashData does not exist on net48; ComputeHash there yields identical bytes.
#if NET5_0_OR_GREATER
        byte[] hash = SHA384.HashData(content);
#else
        using SHA384 sha = SHA384.Create();
        byte[] hash = sha.ComputeHash(content);
#endif
        return "sha384-" + Convert.ToBase64String(hash);
    }

    public static string Compute(string content) => Compute(Encoding.UTF8.GetBytes(content));
}
