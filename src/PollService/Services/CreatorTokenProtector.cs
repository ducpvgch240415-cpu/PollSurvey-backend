using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace PollService.Services;

public interface ICreatorTokenProtector
{
    string CreateToken();
    string Hash(string token);
    bool Verify(string token, string expectedHash);
}

public sealed class CreatorTokenProtector : ICreatorTokenProtector
{
    public string CreateToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public bool Verify(string token, string expectedHash)
    {
        try
        {
            var actualBytes = Convert.FromHexString(Hash(token));
            var expectedBytes = Convert.FromHexString(expectedHash);
            return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
