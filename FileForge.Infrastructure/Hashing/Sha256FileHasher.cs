using System.Security.Cryptography;

namespace FileForge.Infrastructure.Hashing;

public sealed class Sha256FileHasher
{
    public string ComputeHash(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);

        using SHA256 sha256 = SHA256.Create();

        byte[] hashBytes = sha256.ComputeHash(stream);

        return Convert.ToHexString(hashBytes);
    }
}