using Meyers.Core.Models;
using Meyers.Infrastructure.Data;
using Meyers.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Meyers.Test;

public class CalendarDownloadLogTests
{
    private MenuDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MenuDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var context = new MenuDbContext(options);
        context.Database.OpenConnection();
        context.Database.Migrate();
        return context;
    }

    private async Task SeedDownloadLogs(MenuDbContext context, int count, DateTime baseTimestamp,
        string feedPath = "almanak", string clientName = "Apple Calendar", string ipHash = "ABCDEF1234567890")
    {
        for (var i = 0; i < count; i++)
        {
            context.CalendarDownloadLogs.Add(new CalendarDownloadLog
            {
                Timestamp = baseTimestamp.AddHours(-i),
                FeedPath = feedPath,
                ClientName = clientName,
                IpHash = ipHash,
                NotModified = false
            });
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task LogCalendarDownloadAsync_PersistsLog()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);

        await repository.LogCalendarDownloadAsync(new CalendarDownloadLog
        {
            Timestamp = DateTime.UtcNow,
            FeedPath = "almanak",
            ClientName = "Google Calendar",
            IpHash = "ABCDEF1234567890",
            NotModified = false
        });

        Assert.Equal(1, await context.CalendarDownloadLogs.CountAsync());
    }

    [Fact]
    public async Task GetRecentCalendarDownloadsAsync_ReturnsInReverseChronologicalOrder()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var now = DateTime.UtcNow;

        await SeedDownloadLogs(context, 5, now);

        var results = await repository.GetRecentCalendarDownloadsAsync(5);

        Assert.Equal(5, results.Count);
        Assert.True(results[0].Timestamp >= results[1].Timestamp);
        Assert.True(results[1].Timestamp >= results[2].Timestamp);
    }

    [Fact]
    public async Task GetRecentCalendarDownloadsAsync_RespectsLimit()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);

        await SeedDownloadLogs(context, 10, DateTime.UtcNow);

        var results = await repository.GetRecentCalendarDownloadsAsync(3);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task GetCalendarDownloadCountAsync_CountsSinceDate()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var now = DateTime.UtcNow;

        // 3 logs from today, 2 from 10 days ago
        await SeedDownloadLogs(context, 3, now);
        await SeedDownloadLogs(context, 2, now.AddDays(-10), ipHash: "1111111111111111");

        var countToday = await repository.GetCalendarDownloadCountAsync(now.Date);
        var countWeek = await repository.GetCalendarDownloadCountAsync(now.AddDays(-7));
        var countAll = await repository.GetCalendarDownloadCountAsync(now.AddDays(-30));

        Assert.Equal(3, countToday);
        Assert.Equal(3, countWeek);
        Assert.Equal(5, countAll);
    }

    [Fact]
    public async Task GetCalendarDownloadTotalCountAsync_CountsAll()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);

        await SeedDownloadLogs(context, 7, DateTime.UtcNow);

        Assert.Equal(7, await repository.GetCalendarDownloadTotalCountAsync());
    }

    [Fact]
    public async Task GetCalendarDownloadTotalCountAsync_WithNoData_ReturnsZero()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);

        Assert.Equal(0, await repository.GetCalendarDownloadTotalCountAsync());
    }

    [Fact]
    public async Task GetUniqueCalendarDownloadIpsCountAsync_CountsDistinctIps()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var now = DateTime.UtcNow;

        await SeedDownloadLogs(context, 3, now, ipHash: "AAAAAAAAAAAAAAAA");
        await SeedDownloadLogs(context, 2, now.AddMinutes(-10), ipHash: "BBBBBBBBBBBBBBBB");
        await SeedDownloadLogs(context, 1, now.AddMinutes(-20), ipHash: "CCCCCCCCCCCCCCCC");

        var uniqueCount = await repository.GetUniqueCalendarDownloadIpsCountAsync(now.AddDays(-1));

        Assert.Equal(3, uniqueCount);
    }

    [Fact]
    public async Task GetDailyDownloadCountsAsync_GroupsByDate()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var today = DateTime.UtcNow.Date;

        // 3 today, 2 yesterday
        await SeedDownloadLogs(context, 3, today.AddHours(12));
        await SeedDownloadLogs(context, 2, today.AddDays(-1).AddHours(12), ipHash: "1111111111111111");

        var daily = await repository.GetDailyDownloadCountsAsync(today.AddDays(-7));

        Assert.Equal(2, daily.Count);
        Assert.Equal(3, daily[today]);
        Assert.Equal(2, daily[today.AddDays(-1)]);
    }

    [Fact]
    public async Task GetHourlyDownloadCountsAsync_GroupsByHour()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var today = DateTime.UtcNow.Date;

        // 2 at 10:00, 3 at 14:00
        context.CalendarDownloadLogs.AddRange(
            new CalendarDownloadLog { Timestamp = today.AddHours(10), FeedPath = "a", ClientName = "c", IpHash = "1111111111111111" },
            new CalendarDownloadLog { Timestamp = today.AddHours(10).AddMinutes(30), FeedPath = "a", ClientName = "c", IpHash = "2222222222222222" },
            new CalendarDownloadLog { Timestamp = today.AddHours(14), FeedPath = "a", ClientName = "c", IpHash = "3333333333333333" },
            new CalendarDownloadLog { Timestamp = today.AddHours(14).AddMinutes(15), FeedPath = "a", ClientName = "c", IpHash = "4444444444444444" },
            new CalendarDownloadLog { Timestamp = today.AddHours(14).AddMinutes(45), FeedPath = "a", ClientName = "c", IpHash = "5555555555555555" }
        );
        await context.SaveChangesAsync();

        var hourly = await repository.GetHourlyDownloadCountsAsync(today.AddDays(-1));

        Assert.Equal(2, hourly[10]);
        Assert.Equal(3, hourly[14]);
    }

    [Fact]
    public async Task GetTopDownloadClientsAsync_ReturnsOrderedByCount()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var now = DateTime.UtcNow;

        await SeedDownloadLogs(context, 5, now, clientName: "Apple Calendar", ipHash: "AAAAAAAAAAAAAAAA");
        await SeedDownloadLogs(context, 3, now.AddMinutes(-10), clientName: "Google Calendar", ipHash: "BBBBBBBBBBBBBBBB");
        await SeedDownloadLogs(context, 1, now.AddMinutes(-20), clientName: "Thunderbird", ipHash: "CCCCCCCCCCCCCCCC");

        var topClients = await repository.GetTopDownloadClientsAsync(now.AddDays(-1));

        Assert.Equal(3, topClients.Count);
        Assert.Equal("Apple Calendar", topClients[0].Name);
        Assert.Equal(5, topClients[0].Count);
        Assert.Equal("Google Calendar", topClients[1].Name);
        Assert.Equal(3, topClients[1].Count);
        Assert.Equal("Thunderbird", topClients[2].Name);
        Assert.Equal(1, topClients[2].Count);
    }

    [Fact]
    public async Task GetTopDownloadClientsAsync_RespectsLimit()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var now = DateTime.UtcNow;

        await SeedDownloadLogs(context, 3, now, clientName: "Client A", ipHash: "AAAAAAAAAAAAAAAA");
        await SeedDownloadLogs(context, 2, now.AddMinutes(-10), clientName: "Client B", ipHash: "BBBBBBBBBBBBBBBB");
        await SeedDownloadLogs(context, 1, now.AddMinutes(-20), clientName: "Client C", ipHash: "CCCCCCCCCCCCCCCC");

        var topClients = await repository.GetTopDownloadClientsAsync(now.AddDays(-1), limit: 2);

        Assert.Equal(2, topClients.Count);
    }

    [Fact]
    public async Task GetTopDownloadFeedsAsync_ReturnsOrderedByCount()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var now = DateTime.UtcNow;

        await SeedDownloadLogs(context, 5, now, feedPath: "almanak", ipHash: "AAAAAAAAAAAAAAAA");
        await SeedDownloadLogs(context, 2, now.AddMinutes(-10), feedPath: "det-velkendte", ipHash: "BBBBBBBBBBBBBBBB");
        await SeedDownloadLogs(context, 1, now.AddMinutes(-20), feedPath: "custom/M1T1W1R2F1", ipHash: "CCCCCCCCCCCCCCCC");

        var topFeeds = await repository.GetTopDownloadFeedsAsync(now.AddDays(-1));

        Assert.Equal(3, topFeeds.Count);
        Assert.Equal("almanak", topFeeds[0].Name);
        Assert.Equal(5, topFeeds[0].Count);
        Assert.Equal("det-velkendte", topFeeds[1].Name);
    }

    [Fact]
    public async Task GetDownloadSubscribersAsync_GroupsByIpHash()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var now = DateTime.UtcNow;

        // IP A downloads almanak 3 times
        await SeedDownloadLogs(context, 3, now, feedPath: "almanak", clientName: "Apple Calendar", ipHash: "AAAAAAAAAAAAAAAA");
        // IP A also downloads det-velkendte once
        context.CalendarDownloadLogs.Add(new CalendarDownloadLog
        {
            Timestamp = now.AddMinutes(-5),
            FeedPath = "det-velkendte",
            ClientName = "Apple Calendar",
            IpHash = "AAAAAAAAAAAAAAAA"
        });
        // IP B downloads almanak twice
        await SeedDownloadLogs(context, 2, now.AddMinutes(-10), feedPath: "almanak", clientName: "Google Calendar", ipHash: "BBBBBBBBBBBBBBBB");
        await context.SaveChangesAsync();

        var subscribers = await repository.GetDownloadSubscribersAsync(now.AddDays(-1));

        Assert.Equal(2, subscribers.Count);
        // IP A should be first (4 downloads)
        Assert.Equal("AAAAAAAAAAAAAAAA", subscribers[0].IpHash);
        Assert.Equal(4, subscribers[0].Count);
        Assert.Equal("Apple Calendar", subscribers[0].Client);
        Assert.Contains("almanak", subscribers[0].Feeds);
        Assert.Contains("det-velkendte", subscribers[0].Feeds);
        // IP B second (2 downloads)
        Assert.Equal("BBBBBBBBBBBBBBBB", subscribers[1].IpHash);
        Assert.Equal(2, subscribers[1].Count);
    }

    [Fact]
    public async Task GetDownloadSubscribersAsync_WithNoData_ReturnsEmpty()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);

        var subscribers = await repository.GetDownloadSubscribersAsync(DateTime.UtcNow.AddDays(-30));

        Assert.Empty(subscribers);
    }

    [Fact]
    public async Task GetDownloadSubscribersAsync_IncludesFirstAndLastSeen()
    {
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var now = DateTime.UtcNow;
        var earlier = now.AddHours(-5);

        context.CalendarDownloadLogs.AddRange(
            new CalendarDownloadLog { Timestamp = earlier, FeedPath = "a", ClientName = "c", IpHash = "AAAAAAAAAAAAAAAA" },
            new CalendarDownloadLog { Timestamp = now, FeedPath = "a", ClientName = "c", IpHash = "AAAAAAAAAAAAAAAA" }
        );
        await context.SaveChangesAsync();

        var subscribers = await repository.GetDownloadSubscribersAsync(now.AddDays(-1));

        Assert.Single(subscribers);
        Assert.Equal(earlier, subscribers[0].FirstSeen, TimeSpan.FromSeconds(1));
        Assert.Equal(now, subscribers[0].LastSeen, TimeSpan.FromSeconds(1));
    }
}
