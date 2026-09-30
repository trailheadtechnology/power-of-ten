namespace SampleApp.Rule1;

public sealed record Node(Guid Id, IReadOnlyList<Node> Children);

public static class TreeWalker
{
    // ❌ PT0001: stack depth depends on the shape of the data
    public static IEnumerable<Node> Flatten(Node root)
    {
        yield return root;
        foreach (var child in root.Children)
        {
            foreach (var n in Flatten(child))
                yield return n;
        }
    }

    // ✅ explicit loop, cycle guard, and a hard cap on work
    public static IEnumerable<Node> FlattenSafely(Node root, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(root);
        const int maxNodes = 100_000;
        var stack = new Stack<Node>();
        var visited = new HashSet<Guid>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var node = stack.Pop();
            if (!visited.Add(node.Id)) continue;
            if (visited.Count > maxNodes) throw new InvalidOperationException("Tree is too large.");
            yield return node;
            foreach (var child in node.Children) stack.Push(child);
        }
    }
}
