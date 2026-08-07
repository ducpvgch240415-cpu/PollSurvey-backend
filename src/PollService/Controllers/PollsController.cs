using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PollService.Data;
using PollService.Domain;
using PollService.DTOs;
using PollService.Services;

namespace PollService.Controllers;

[ApiController]
[Route("polls")]
public sealed class PollsController(
    PollDbContext dbContext,
    IShortCodeGenerator codeGenerator,
    ICreatorTokenProtector tokenProtector) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PollResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PollResponse>>> List(
    [FromQuery] int limit = 20,
    CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var polls = await dbContext.Polls
            .AsNoTracking()
            .Include(poll => poll.Options)
            .Where(poll => poll.DeletedAt == null)
            .OrderByDescending(poll => poll.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return Ok(polls.Select(ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType<CreatePollResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePollResponse>> Create(
        [FromBody] CreatePollRequest request,
        CancellationToken cancellationToken)
    {
        var errors = PollInputValidator.Validate(request);
        if (errors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(errors)
            {
                Title = "The poll data is invalid.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var code = await CreateUniqueCode(cancellationToken);
        var creatorToken = tokenProtector.CreateToken();
        var createdAt = DateTimeOffset.UtcNow;

        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            Code = code,
            Question = request.Question.Trim(),
            IsClosed = false,
            CreatedAt = createdAt,
            CreatorTokenHash = tokenProtector.Hash(creatorToken),
            Options = request.Options.Select((text, position) => new PollOption
            {
                Id = Guid.NewGuid(),
                Text = text.Trim(),
                Position = position
            }).ToList()
        };

        dbContext.Polls.Add(poll);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CreatePollResponse(code, creatorToken, $"/poll/{code}", createdAt);
        return CreatedAtAction(nameof(Get), new { code }, response);
    }

    [HttpGet("{code}")]
    [ProducesResponseType<PollResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PollResponse>> Get(
        string code,
        CancellationToken cancellationToken)
    {
        var poll = await dbContext.Polls
            .AsNoTracking()
            .Include(item => item.Options)
            .SingleOrDefaultAsync(
                item => item.Code == code && item.DeletedAt == null,
                cancellationToken);

        if (poll is null)
            return NotFound();

        return Ok(ToResponse(poll));
    }

    [HttpPut("{code}")]
    [ProducesResponseType<PollResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PollResponse>> Update(
        string code,
        [FromBody] UpdatePollRequest request,
        CancellationToken cancellationToken)
    {
        var poll = await dbContext.Polls
            .Include(item => item.Options)
            .SingleOrDefaultAsync(
                item => item.Code == code && item.DeletedAt == null,
                cancellationToken);

        if (poll is null)
            return NotFound();

        if (!IsCreator(request.CreatorToken, poll))
            return StatusCode(StatusCodes.Status403Forbidden);

        if (poll.IsClosed)
        {
            return Conflict(new ProblemDetails
            {
                Title = "A closed poll cannot be edited."
            });
        }

        var errors = PollInputValidator.Validate(request);
        if (errors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(errors)
            {
                Title = "The poll data is invalid.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var existingOptions = poll.Options
    .OrderBy(option => option.Position)
    .ToList();

        // Removing options is still blocked because they may already have votes.
        if (request.Options.Count < existingOptions.Count)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Existing options cannot be removed.",
                Detail = "Removing an option could invalidate votes already submitted."
            });
        }

        poll.Question = request.Question.Trim();

        // Update text for the options that already exist.
        for (var position = 0; position < existingOptions.Count; position++)
        {
            existingOptions[position].Text = request.Options[position].Trim();
        }

        // Add any new options after the existing ones.
        for (var position = existingOptions.Count; position < request.Options.Count; position++)
        {
            poll.Options.Add(new PollOption
            {
                Text = request.Options[position].Trim(),
                Position = position
            });
        }

        poll.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(poll));
    }

    [HttpPatch("{code}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(
        string code,
        [FromBody] ClosePollRequest request,
        CancellationToken cancellationToken)
    {
        var poll = await dbContext.Polls
            .SingleOrDefaultAsync(
                item => item.Code == code && item.DeletedAt == null,
                cancellationToken);

        if (poll is null)
            return NotFound();

        if (!IsCreator(request.CreatorToken, poll))
            return StatusCode(StatusCodes.Status403Forbidden);

        if (!poll.IsClosed)
        {
            poll.IsClosed = true;
            poll.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpDelete("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        string code,
        [FromHeader(Name = "X-Creator-Token")] string? creatorToken,
        CancellationToken cancellationToken)
    {
        var poll = await dbContext.Polls
            .SingleOrDefaultAsync(
                item => item.Code == code && item.DeletedAt == null,
                cancellationToken);

        if (poll is null)
            return NotFound();

        if (!IsCreator(creatorToken, poll))
            return StatusCode(StatusCodes.Status403Forbidden);

        poll.IsClosed = true;
        poll.DeletedAt = DateTimeOffset.UtcNow;
        poll.UpdatedAt = poll.DeletedAt;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<string> CreateUniqueCode(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = codeGenerator.Generate();
            var exists = await dbContext.Polls.AnyAsync(
                poll => poll.Code == code,
                cancellationToken);

            if (!exists)
                return code;
        }

        throw new InvalidOperationException("A unique poll code could not be generated.");
    }

    private static PollResponse ToResponse(Poll poll) => new(
        poll.Code,
        poll.Question,
        poll.IsClosed,
        poll.CreatedAt,
        poll.UpdatedAt,
        poll.Options
            .OrderBy(option => option.Position)
            .Select(option => new PollOptionResponse(option.Id, option.Text, option.Position))
            .ToList());

    private bool IsCreator(string? creatorToken, Poll poll) =>
        !string.IsNullOrWhiteSpace(creatorToken) &&
        tokenProtector.Verify(creatorToken, poll.CreatorTokenHash);
}
