namespace SampleApp.Rule2;

public sealed class StatusClient
{
    public Task<string> GetStatusAsync(CancellationToken ct) => Task.FromResult("Ready");
}

public sealed class DeploymentWatcher(StatusClient client)
{
    // ❌ PT0002: assumes "Ready" will eventually happen
    public async Task WaitForReadyAsync(CancellationToken ct)
    {
        while (true)
        {
            var status = await client.GetStatusAsync(ct);
            if (status == "Ready") break;
            await Task.Delay(500, ct);
        }
    }

    // ✅ fixed bound, and failure is explicit
    public async Task WaitForReadyBoundedAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const int maxAttempts = 20;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var status = await client.GetStatusAsync(ct);
            if (status == "Ready") return;
            await Task.Delay(500, ct);
        }
        throw new TimeoutException($"Not ready after {maxAttempts} attempts.");
    }

    // ❌ PT0002: for (;;) is while (true) in disguise
    public static int Drain(Queue<int> queue)
    {
        var total = 0;
        for (;;)
        {
            if (!queue.TryDequeue(out var item)) return total;
            total += item;
        }
    }

    // ❌ PT0002: so is do/while (true)
    public static int Retry(Func<bool> operation)
    {
        var attempts = 0;
        do
        {
            attempts++;
            if (operation()) return attempts;
        } while (true);
    }
}
