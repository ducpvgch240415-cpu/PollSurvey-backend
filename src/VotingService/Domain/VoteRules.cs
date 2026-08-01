using VotingService.Clients;

namespace VotingService.Domain;

public enum VoteValidationError
{
    None,
    PollClosed,
    InvalidOption
}

public static class VoteRules
{
    public static VoteValidationError Validate(PollContract poll, Guid optionId)
    {
        if (poll.IsClosed)
            return VoteValidationError.PollClosed;

        return poll.Options.Any(option => option.Id == optionId)
            ? VoteValidationError.None
            : VoteValidationError.InvalidOption;
    }
}

