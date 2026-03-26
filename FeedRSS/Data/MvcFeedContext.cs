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
    }
}
