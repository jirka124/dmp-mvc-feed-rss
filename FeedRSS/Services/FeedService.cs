using FeedRSS.Data;
using FeedRSS.Models;
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

    public Task<Feed?> GetByIdAsync(int id, bool includeArticles = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Feed> query = _context.Feed;
        if (includeArticles)
        {
            query = query.Include(f => f.Articles);
        }

        return query.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

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
}
