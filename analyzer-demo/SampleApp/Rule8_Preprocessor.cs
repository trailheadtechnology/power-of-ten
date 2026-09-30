namespace SampleApp.Rule8;

public static class Checkout
{
    // ❌ PT0008 (reported on the first #if): 4 symbols = 2^4 = 16 builds to test
    public static decimal Total(decimal subtotal)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(subtotal);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(subtotal, 1_000_000m);
#if FEATURE_DISCOUNTS
        subtotal *= 0.9m;
#endif
#if FEATURE_TAX
        subtotal *= 1.06m;
#endif
#if FEATURE_SURCHARGE
        subtotal += 2.50m;
#endif
#if FEATURE_ROUNDING
        subtotal = Math.Round(subtotal, 2);
#endif
        return subtotal;
    }
}

// ✅ one build; the variants are runtime data you can test in a single test run
public sealed record CheckoutFeatures(bool Discounts, bool Tax, bool Surcharge, bool Rounding);

public static class FlaggedCheckout
{
    public static decimal Total(decimal subtotal, CheckoutFeatures f)
    {
        if (f.Discounts) subtotal *= 0.9m;
        if (f.Tax) subtotal *= 1.06m;
        if (f.Surcharge) subtotal += 2.50m;
        if (f.Rounding) subtotal = Math.Round(subtotal, 2);
        return subtotal;
    }
}
