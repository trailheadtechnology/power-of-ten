namespace SampleApp.Rule3;

public sealed record Order(Guid Id, DateTime CreatedAtUtc, decimal Total);

// Stands in for an EF Core DbContext; ToListAsync() on a real DbSet is flagged the same way
public sealed class OrderStore
{
    public IQueryable<Order> Orders { get; } = new List<Order>().AsQueryable();
}

public sealed class OrderReports(OrderStore db)
{
    // ❌ PT0003: time-bound, but not volume-bound; a busy day loads every row
    public List<Order> GetOrdersLastDay()
    {
        var since = DateTime.UtcNow.AddDays(-1);
        return db.Orders
            .Where(o => o.CreatedAtUtc >= since)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToList();
    }

    // ✅ the caller asks for a page, and the page size is clamped
    public List<Order> GetOrdersLastDay(int limit)
    {
        limit = Math.Clamp(limit, 1, 200);
        var since = DateTime.UtcNow.AddDays(-1);
        return db.Orders
            .Where(o => o.CreatedAtUtc >= since)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(limit)
            .ToList();
    }
}
