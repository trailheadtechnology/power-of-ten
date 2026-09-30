namespace SampleApp.Rule4;

public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);

public sealed class Order
{
    public Guid Id { get; init; }
    public string CustomerEmail { get; init; } = "";
    public string State { get; init; } = "";
    public List<OrderLine> Lines { get; init; } = [];
    public string Status { get; set; } = "Open";
    public decimal Total { get; set; }
}

public sealed class OrderProcessor
{
    private readonly List<string> _outbox = [];

    // ❌ PT0004: validation, pricing, tax, payment, and email in one method
    public void ProcessOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Status == "Closed") return;
        if (order.Lines.Count == 0) throw new InvalidOperationException("Empty order");

        // pricing
        decimal subtotal = 0;
        foreach (var line in order.Lines)
        {
            if (line.Quantity <= 0) throw new InvalidOperationException($"Bad quantity for {line.Sku}");
            if (line.UnitPrice < 0) throw new InvalidOperationException($"Bad price for {line.Sku}");
            subtotal += line.Quantity * line.UnitPrice;
        }

        // discounts
        decimal discount = 0;
        if (subtotal > 500) discount = subtotal * 0.10m;
        else if (subtotal > 100) discount = subtotal * 0.05m;
        var discounted = subtotal - discount;

        // tax
        decimal taxRate = order.State switch
        {
            "MI" => 0.06m,
            "OH" => 0.0575m,
            "IN" => 0.07m,
            _ => 0m,
        };
        var tax = Math.Round(discounted * taxRate, 2);
        var total = discounted + tax;

        // payment
        if (total > 10_000) throw new InvalidOperationException("Needs manual approval");
        var paymentId = Guid.NewGuid();
        _outbox.Add($"charge {order.CustomerEmail} {total:C} ref {paymentId}");

        // close
        order.Total = total;
        order.Status = "Closed";

        // receipt
        var receipt = $"Order {order.Id}\nSubtotal {subtotal:C}\nDiscount {discount:C}\nTax {tax:C}\nTotal {total:C}";
        _outbox.Add($"email {order.CustomerEmail}: {receipt}");
    }

    // ✅ an orchestrator whose steps each do one job
    public void CloseOrder(Order order)
    {
        EnsureClosable(order);
        var total = CalculateTotal(order);
        Charge(order, total);
        order.Total = total;
        order.Status = "Closed";
        SendReceipt(order);
    }

    private static void EnsureClosable(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Lines.Count == 0) throw new InvalidOperationException("Empty order");
    }

    private static decimal CalculateTotal(Order order)
        => order.Lines.Sum(l => l.Quantity * l.UnitPrice);

    private void Charge(Order order, decimal total)
        => _outbox.Add($"charge {order.CustomerEmail} {total:C}");

    private void SendReceipt(Order order)
        => _outbox.Add($"email {order.CustomerEmail}: total {order.Total:C}");
}
