using PollService.DTOs;
using PollService.Services;

namespace PollService.UnitTests;

public sealed class PollInputValidatorTests
{
    [Fact]
    public void Validate_WithValidPoll_ReturnsNoErrors()
    {
        var request = new CreatePollRequest(
            "Which option do you prefer?",
            ["Option A", "Option B"]);

        var errors = PollInputValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithOnlyOneOption_ReturnsOptionsError()
    {
        var request = new CreatePollRequest("Question", ["Only option"]);

        var errors = PollInputValidator.Validate(request);

        Assert.Contains(nameof(request.Options), errors.Keys);
    }

    [Fact]
    public void Validate_WithMoreThanSixOptions_ReturnsOptionsError()
    {
        var request = new CreatePollRequest(
            "Question",
            ["1", "2", "3", "4", "5", "6", "7"]);

        var errors = PollInputValidator.Validate(request);

        Assert.Contains(nameof(request.Options), errors.Keys);
    }

    [Fact]
    public void Validate_WithCaseInsensitiveDuplicateOptions_ReturnsOptionsError()
    {
        var request = new CreatePollRequest("Question", ["Yes", " yes "]);

        var errors = PollInputValidator.Validate(request);

        Assert.Contains(nameof(request.Options), errors.Keys);
    }

    [Fact]
    public void Validate_WithBlankQuestion_ReturnsQuestionError()
    {
        var request = new CreatePollRequest("  ", ["Yes", "No"]);

        var errors = PollInputValidator.Validate(request);

        Assert.Contains(nameof(request.Question), errors.Keys);
    }

    [Fact]
    public void Validate_WithValidUpdate_ReturnsNoErrors()
    {
        var request = new UpdatePollRequest(
            "creator-token",
            "Updated question?",
            ["Updated A", "Updated B"]);

        var errors = PollInputValidator.Validate(request);

        Assert.Empty(errors);
    }
}
