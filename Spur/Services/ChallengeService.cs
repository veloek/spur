using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore;
using Spur.Data;
using Spur.Model;

namespace Spur.Services;

public class ChallengeService : IChallengeService
{
    private readonly ILogger<ChallengeService> _logger;
    private readonly IDbContextFactory<DataContext> _dataContextFactory;
    private readonly Subject<FeedUpdate<Challenge>> _challengeFeed = new();

    public ChallengeService(
        ILogger<ChallengeService> logger,
        IDbContextFactory<DataContext> dataContextFactory)
    {
        _logger = logger;
        _dataContextFactory = dataContextFactory;
    }

    public async Task<Challenge[]> GetChallengesForAthleteAsync(int athleteId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge[] challenges = await dataContext.Challenges
            .Include(challenge => challenge.Athletes)
            .Include(challenge => challenge.CreatedBy)
            .Where(challenge =>
                challenge.Athletes!.Any(athlete => athlete.Id == athleteId)
                || challenge.CreatedById == athleteId)
            .ToArrayAsync(ct);

        return challenges;
    }

    public IObservable<FeedUpdate<Challenge>> GetChallengeFeedForAthlete(int athleteId)
    {
        return _challengeFeed
            .Where(update =>
                update.Item.Athletes?.Any(athlete => athlete.Id == athleteId) == true
                || update.Item.CreatedById == athleteId);
    }

    public async Task<Challenge> CreateChallengeAsync(ChallengeFormModel newChallenge, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        _logger.LogInformation("Adding new challenge: {Title}", newChallenge.Title);

        List<Athlete> athletes = dataContext.Athletes
            .Where(a => newChallenge.Athletes.Contains(a.Id))
            .AsTracking()
            .ToList();

        Challenge challenge = await dataContext.AddChallengeAsync(new Challenge()
        {
            Title = newChallenge.Title,
            Description = newChallenge.Description,
            Start = newChallenge.Start,
            End = newChallenge.End,
            Measurement = newChallenge.Measurement,
            ActivityTypes = newChallenge.ActivityTypes,
            Created = DateTimeOffset.Now,
            CreatedById = newChallenge.CreatedBy,
            Athletes = athletes,
        }, ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Create });

        return challenge;
    }

    public async Task<Challenge> UpdateChallengeAsync(int challengeId, ChallengeFormModel editChallenge,
        CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
            .Include(c => c.Athletes)
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct) ??
            throw new Exception($"Unknown challenge ID {challengeId}");

        _logger.LogInformation("Updating challenge ID {ChallengeId}", challengeId);

        List<Athlete> athletes = dataContext.Athletes
            .Where(a => editChallenge.Athletes.Contains(a.Id))
            .AsTracking()
            .ToList();

        challenge.Title = editChallenge.Title;
        challenge.Description = editChallenge.Description;
        challenge.Start = editChallenge.Start;
        challenge.End = editChallenge.End;
        challenge.Measurement = editChallenge.Measurement;
        challenge.ActivityTypes = editChallenge.ActivityTypes;
        challenge.Athletes = athletes;

        challenge = await dataContext.UpdateChallengeAsync(challenge, CancellationToken.None);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Update });

        return challenge;
    }

    public async Task DeleteChallengeAsync(int challengeId, CancellationToken ct = default)
    {
        using DataContext dataContext = await _dataContextFactory.CreateDbContextAsync(ct);

        Challenge challenge = await dataContext.Challenges
            .Include(c => c.Athletes)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct) ??
            throw new Exception($"Unknown challenge ID {challengeId}");

        _logger.LogInformation($"Deleting challenge ID {challengeId}");
        _ = await dataContext.RemoveChallengeAsync(challenge, ct);

        _challengeFeed.OnNext(new FeedUpdate<Challenge> { Item = challenge, Action = FeedAction.Delete });
    }
}
