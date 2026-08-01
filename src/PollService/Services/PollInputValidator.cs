using PollService.DTOs;

namespace PollService.Services;

public static class PollInputValidator
{
    public static Dictionary<string, string[]> Validate(CreatePollRequest request) =>
        Validate(request.Question, request.Options);

    public static Dictionary<string, string[]> Validate(UpdatePollRequest request) =>
        Validate(request.Question, request.Options);

    private static Dictionary<string, string[]> Validate(
        string question,
        IReadOnlyList<string>? options)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(question))
            errors["Question"] = ["Question is required."];
        else if (question.Trim().Length > 500)
            errors["Question"] = ["Question cannot exceed 500 characters."];

        if (options is null || options.Count is < 2 or > 6)
        {
            errors["Options"] = ["A poll must contain between 2 and 6 options."];
            return errors;
        }

        if (options.Any(string.IsNullOrWhiteSpace))
            errors["Options"] = ["Options cannot be empty."];
        else if (options.Any(option => option.Trim().Length > 200))
            errors["Options"] = ["Each option cannot exceed 200 characters."];
        else if (options.Select(option => option.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Count)
            errors["Options"] = ["Options must be unique."];

        return errors;
    }
}
