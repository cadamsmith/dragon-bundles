using System.Security.Cryptography;
using System.Text;

namespace DragonBundles.Tests;

public class SriHashTests
{
    [Fact]
    public void Compute_MatchesIndependentSha384Base64()
    {
        const string content = "body{color:red}";
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        string expected = "sha384-" + Convert.ToBase64String(SHA384.HashData(bytes));

        Assert.Equal(expected, SriHash.Compute(content));
        Assert.Equal(expected, SriHash.Compute(bytes));
    }

    [Fact]
    public void Compute_HasSha384Prefix()
    {
        Assert.StartsWith("sha384-", SriHash.Compute("x"));
    }

    [Fact]
    public void Compute_IsDeterministicAndContentSensitive()
    {
        Assert.Equal(SriHash.Compute("a"), SriHash.Compute("a"));
        Assert.NotEqual(SriHash.Compute("a"), SriHash.Compute("b"));
    }
}
