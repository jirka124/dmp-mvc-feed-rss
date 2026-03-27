using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FeedRSS.Models;

namespace FeedRSS.Data
{
    public class MvcFeedContext : DbContext
    {
        public MvcFeedContext (DbContextOptions<MvcFeedContext> options)
            : base(options)
        {
        }

        public DbSet<FeedRSS.Models.Feed> Feed { get; set; } = default!;
        public DbSet<FeedRSS.Models.Article> Article { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Article>()
                .HasIndex(a => new { a.FeedId, a.Link })
                .IsUnique();

            modelBuilder.Entity<Article>()
                .HasIndex(a => new { a.FeedId, a.PublishedAt });
        }
    }
}
