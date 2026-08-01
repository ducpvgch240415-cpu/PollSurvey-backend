using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

namespace RealtimeService.Hubs;

public sealed partial class PollHub : Hub
{
    public Task WatchPoll(string code) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(code));

    public Task StopWatchingPoll(string code) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(code));

    public static string GroupName(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || !PollCodePattern().IsMatch(code))
            throw new HubException("The poll code is invalid.");

        return $"poll:{code}";
    }

    [GeneratedRegex("^[A-Za-z0-9]{5,12}$")]
    private static partial Regex PollCodePattern();
}

