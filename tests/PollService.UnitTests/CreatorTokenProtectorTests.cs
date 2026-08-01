using PollService.Services;

namespace PollService.UnitTests;

public sealed class CreatorTokenProtectorTests
{
    [Fact]
    public void Verify_WithOriginalToken_ReturnsTrue()
    {
        var protector = new CreatorTokenProtector();
        var token = protector.CreateToken();
        var hash = protector.Hash(token);

        Assert.True(protector.Verify(token, hash));
    }

    [Fact]
    public void Verify_WithDifferentToken_ReturnsFalse()
    {
        var protector = new CreatorTokenProtector();
        var hash = protector.Hash(protector.CreateToken());

        Assert.False(protector.Verify(protector.CreateToken(), hash));
    }
}

