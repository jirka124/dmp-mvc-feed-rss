using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FeedRSS.Models;
using FeedRSS.Services;

namespace FeedRSS.Controllers
{
    public class FeedController : Controller
    {
        private readonly IFeedService _feedService;
        private readonly IRssService _rssService;

        public FeedController(IFeedService feedService, IRssService rssService)
        {
            _feedService = feedService;
            _rssService = rssService;
        }

        // GET: Feed
        public async Task<IActionResult> Index()
        {
            return View(await _feedService.GetAllAsync());
        }

        // GET: Feed/Details/5
        public async Task<IActionResult> Details(int? id, DateOnly? from, DateOnly? to)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (from.HasValue && to.HasValue && from > to)
            {
                ModelState.AddModelError(string.Empty, "'From' date must be earlier than or equal to 'To' date.");
            }

            var details = await _feedService.GetDetailsAsync(id.Value, from, to);
            if (details == null)
            {
                return NotFound();
            }

            return View(details);
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
                await _feedService.CreateAsync(feed);
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

            var feed = await _feedService.GetByIdAsync(id.Value);
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
                    await _feedService.UpdateAsync(feed);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _feedService.ExistsAsync(feed.Id))
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

            var feed = await _feedService.GetByIdAsync(id.Value);
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
            await _feedService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reload(int id, CancellationToken cancellationToken)
        {
            try
            {
                var added = await _rssService.ReloadFeedAsync(id, cancellationToken);
                TempData["StatusMessage"] = $"Feed reloaded successfully. Added {added} new article(s).";
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                TempData["StatusMessage"] = "Feed reload failed. Please check the feed URL and try again.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
