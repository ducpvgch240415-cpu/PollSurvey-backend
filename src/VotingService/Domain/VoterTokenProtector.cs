using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace VotingService.Domain;

public interface IVoterTokenProtector
{
    string CreateToken();
    string Hash(string token);
}

public sealed class VoterTokenProtector : IVoterTokenProtector
{
    public string CreateToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

