using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PollSurvey.Contracts;
using VotingService.Clients;
using VotingService.Data;
using VotingService.Domain;
using VotingService.DTOs;
using VotingService.Messaging;

namespace VotingService.Controllers;

[ApiController]
[Route("polls/{code}")]
public sealed class VotesController(
    VoteDbContext dbContext,
    IPollServiceClient pollServiceClient,
    IVoterTokenProtector tokenProtector,
    IVoteEventPublisher eventPublisher,
    ILogger<VotesController> logger) : ControllerBase
{
    private const string VoterCookieName = "poll-voter-token";

    [HttpPost("vote")]
    [ProducesResponseType<PollResults>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PollResults>> Vote(
        string code,
        [FromBody] SubmitVoteRequest request,
        CancellationToken cancellationToken)
    {
        var poll = await pollServiceClient.GetPollAsync(code, cancellationToken);
        if (poll is null)
            return NotFound();

        var validationError = VoteRules.Validate(poll, request.OptionId);
        if (validationError == VoteValidationError.PollClosed)
            return Conflict(new ProblemDetails { Title = "This poll is closed." });
        if (validationError == VoteValidationError.InvalidOption)
            return BadRequest(new ProblemDetails { Title = "The selected option is invalid." });

        var voterToken = GetOrCreateVoterToken();
        var vote = new Vote
        {
            Id = Guid.NewGuid(),
            PollCode = poll.Code,
            OptionId = request.OptionId,
            VoterTokenHash = tokenProtector.Hash(voterToken),
            VotedAt = DateTimeOffset.UtcNow
        };

        dbContext.Votes.Add(vote);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
                  { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict(new ProblemDetails
            {
                Title = "This browser has already voted in this poll."
            });
        }

        var results = await BuildResults(poll, cancellationToken);

        try
        {
            await eventPublisher.PublishAsync(
                new VoteRecordedEvent(poll.Code, vote.VotedAt, results),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Vote {VoteId} was saved, but its realtime event could not be published.",
                vote.Id);
        }

        return Ok(results);
    }

    [HttpGet("results")]
    [ProducesResponseType<PollResults>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PollResults>> Results(
        string code,
        CancellationToken cancellationToken)
    {
        var poll = await pollServiceClient.GetPollAsync(code, cancellationToken);
        if (poll is null)
            return NotFound();

        return Ok(await BuildResults(poll, cancellationToken));
    }

    private string GetOrCreateVoterToken()
    {
        if (Request.Cookies.TryGetValue(VoterCookieName, out var existingToken) &&
            !string.IsNullOrWhiteSpace(existingToken))
        {
            return existingToken;
        }

        var token = tokenProtector.CreateToken();
        Response.Cookies.Append(VoterCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(365)
        });
        return token;
    }

    private async Task<PollResults> BuildResults(
        PollContract poll,
        CancellationToken cancellationToken)
    {
        var counts = await dbContext.Votes
            .AsNoTracking()
            .Where(vote => vote.PollCode == poll.Code)
            .GroupBy(vote => vote.OptionId)
            .Select(group => new { OptionId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.OptionId, item => item.Count, cancellationToken);

        var options = poll.Options
            .OrderBy(option => option.Position)
            .Select(option => new PollOptionResult(
                option.Id,
                option.Text,
                option.Position,
                counts.GetValueOrDefault(option.Id)))
            .ToList();

        return new PollResults(poll.Code, options.Sum(option => option.Votes), options);
    }
}
