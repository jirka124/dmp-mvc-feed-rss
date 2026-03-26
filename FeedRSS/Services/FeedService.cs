using FeedRSS.Data;
using FeedRSS.Models;
using FeedRSS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FeedRSS.Services;

public class FeedService : IFeedService
{
    private readonly MvcFeedContext _context;

    public FeedService(MvcFeedContext context)
    {
        _context = context;
    }

    public Task<List<Feed>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.Feed.ToListAsync(cancellationToken);
    }

    public Task<Feed?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Feed.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task CreateAsync(Feed feed, CancellationToken cancellationToken = default)
    {
        _context.Feed.Add(feed);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Feed feed, CancellationToken cancellationToken = default)
    {
        _context.Update(feed);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var feed = await _context.Feed.FindAsync([id], cancellationToken);
        if (feed is null)
        {
            return false;
        }

        _context.Feed.Remove(feed);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Feed.AnyAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<FeedDetailsViewModel?> GetDetailsAsync(
        int id,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var feed = await _context.Feed
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (feed is null)
        {
            return null;
        }

        IQueryable<Article> query = _context.Article
            .Where(a => a.FeedId == id);

        if (from.HasValue)
        {
            var fromStart = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.PublishedAt.HasValue && a.PublishedAt.Value >= fromStart);
        }

        if (to.HasValue)
        {
            var toExclusive = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.PublishedAt.HasValue && a.PublishedAt.Value < toExclusive);
        }

        var articles = await query
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync(cancellationToken);

        return new FeedDetailsViewModel
        {
            Feed = feed,
            Articles = articles,
            From = from,
            To = to
        };
    }
}
