using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FeedRSS.Data;

public static class DataExtensions
{
    public static WebApplicationBuilder AddFeedDb(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("MvcFeedContext")
            ?? throw new InvalidOperationException("Connection string 'MvcFeedContext' not found.");

        EnsureSqliteDirectoryExists(connectionString, builder.Environment.ContentRootPath);

        builder.Services.AddDbContext<MvcFeedContext>(options =>
            options.UseSqlite(
                connectionString)
            .UseSeeding((context, _) =>
            {
                if (context is MvcFeedContext dbContext)
                {
                    DbInitializer.Seed(dbContext);
                }
            }));

        return builder;
    }

    public static WebApplication MigrateDb(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<MvcFeedContext>();

        DbInitializer.Migrate(context);
        return app;
    }

    private static void EnsureSqliteDirectoryExists(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:")
        {
            return;
        }

        var dbPath = Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.Combine(contentRootPath, dataSource);

        var directoryPath = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
