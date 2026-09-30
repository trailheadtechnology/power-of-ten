namespace SampleApp.Rule5;

public sealed class Order
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Total { get; init; }
    public string Status { get; set; } = "Open";
}

public sealed class OrderDb
{
    private readonly Dictionary<Guid, Order> _orders = [];
    public Task<Order?> FindAsync(Guid id) => Task.FromResult(_orders.GetValueOrDefault(id));
    public Task SaveAsync(Order order) => Task.CompletedTask;
}

public sealed class Payments
{
    public Task ChargeAsync(Guid customerId, decimal amount) => Task.CompletedTask;
}

public sealed class OrderCloser(OrderDb db, Payments payments, Action<string> log)
{
    // ❌ PT0005: looks defensive, but it only logs, and the caller thinks it worked
    public async Task CloseOrderAsync(Guid orderId)
    {
        var order = await db.FindAsync(orderId);
        if (order == null)
        {
            log($"Order not found: {orderId}");
            return;
        }
        if (order.Total <= 0)
        {
            log($"Order {orderId} has a non-positive total");
        }
        await payments.ChargeAsync(order.CustomerId, order.Total);
        order.Status = "Closed";
        await db.SaveAsync(order);
    }

    // ✅ preconditions are checked, and failure is returned to the caller
    public async Task<bool> TryCloseOrderAsync(Guid orderId)
    {
        if (orderId == Guid.Empty) return false;
        var order = await db.FindAsync(orderId);
        if (order is null) return false;
        if (order.Status == "Closed") return true;
        if (order.Total <= 0) return false;

        await payments.ChargeAsync(order.CustomerId, order.Total);
        order.Status = "Closed";
        await db.SaveAsync(order);
        return true;
    }
}
