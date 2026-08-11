using System.Security.Cryptography;

namespace PollService.Services;

public interface IShortCodeGenerator
{
    string Generate(int length = 7);
}

public sealed class ShortCodeGenerator : IShortCodeGenerator
{
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    public string Generate(int length = 7)
    {
        if (length is < 5 or > 12)
            throw new ArgumentOutOfRangeException(nameof(length));

        return string.Create(length, 0, static (buffer, _) =>
        {
            for (var index = 0; index < buffer.Length; index++)
                buffer[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        });
    }

}

