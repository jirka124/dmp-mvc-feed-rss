using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FeedRSS.Data;
using FeedRSS.Models;
using FeedRSS.Services;

namespace FeedRSS.Controllers
{
    public class FeedController : Controller
    {
        private readonly MvcFeedContext _context;
        private readonly IRssService _rssService;

        public FeedController(MvcFeedContext context, IRssService rssService)
        {
            _context = context;
            _rssService = rssService;
        }

        // GET: Feed
        public async Task<IActionResult> Index()
        {
            return View(await _context.Feed.ToListAsync());
        }

        // GET: Feed/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feed = await _context.Feed
                .Include(f => f.Articles)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (feed == null)
            {
                return NotFound();
            }

            return View(feed);
        }

        // GET: Feed/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Feed/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Url")] Feed feed)
        {
            if (ModelState.IsValid)
            {
                _context.Add(feed);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(feed);
        }

        // GET: Feed/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feed = await _context.Feed.FindAsync(id);
            if (feed == null)
            {
                return NotFound();
            }
            return View(feed);
        }

        // POST: Feed/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Url")] Feed feed)
        {
            if (id != feed.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(feed);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FeedExists(feed.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(feed);
        }

        // GET: Feed/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feed = await _context.Feed
                .FirstOrDefaultAsync(m => m.Id == id);
            if (feed == null)
            {
                return NotFound();
            }

            return View(feed);
        }

        // POST: Feed/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feed = await _context.Feed.FindAsync(id);
            if (feed != null)
            {
                _context.Feed.Remove(feed);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reload(int id, CancellationToken cancellationToken)
        {
            if (!FeedExists(id))
            {
                return NotFound();
            }

            try
            {
                var added = await _rssService.ReloadFeedAsync(id, cancellationToken);
                TempData["StatusMessage"] = $"Feed reloaded successfully. Added {added} new article(s).";
            }
            catch (Exception)
            {
                TempData["StatusMessage"] = "Feed reload failed. Please check the feed URL and try again.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private bool FeedExists(int id)
        {
            return _context.Feed.Any(e => e.Id == id);
        }
    }
}
