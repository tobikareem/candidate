using System.Security.Cryptography;
using System.Text;

namespace Recon.Domain;

public static class SurrogateKey
{
    private const char Separator = '~';

    public static string For(string entityType, params string?[] naturalKeyParts)
    {
        var canonical = string.Join(Separator, naturalKeyParts.Select(Canonicalize));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return $"{entityType}-{Convert.ToHexString(hash)[..12].ToLowerInvariant()}";
    }

    
    private static string Canonicalize(string? part) => (part ?? string.Empty).Trim().ToLowerInvariant();
}
