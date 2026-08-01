using VotingService.Clients;
using VotingService.Domain;

namespace VotingService.UnitTests;

public sealed class VoteRulesTests
{
    private static readonly Guid FirstOptionId = Guid.NewGuid();

    [Fact]
    public void Validate_WhenPollIsClosed_ReturnsPollClosed()
    {
        var poll = CreatePoll(isClosed: true);

        var result = VoteRules.Validate(poll, FirstOptionId);

        Assert.Equal(VoteValidationError.PollClosed, result);
    }

    [Fact]
    public void Validate_WhenOptionDoesNotExist_ReturnsInvalidOption()
    {
        var poll = CreatePoll(isClosed: false);

        var result = VoteRules.Validate(poll, Guid.NewGuid());

        Assert.Equal(VoteValidationError.InvalidOption, result);
    }

    [Fact]
    public void Validate_WhenPollAndOptionAreValid_ReturnsNone()
    {
        var poll = CreatePoll(isClosed: false);

        var result = VoteRules.Validate(poll, FirstOptionId);

        Assert.Equal(VoteValidationError.None, result);
    }

    private static PollContract CreatePoll(bool isClosed) => new(
        "Abc1234",
        "Question",
        isClosed,
        DateTimeOffset.UtcNow,
        [
            new PollOptionContract(FirstOptionId, "Yes", 0),
            new PollOptionContract(Guid.NewGuid(), "No", 1)
        ]);
}

