using static PowerOfTen.Analyzers.Tests.AnalyzerRunner;

namespace PowerOfTen.Analyzers.Tests;

public class DirectRecursionTests
{
    [Fact]
    public async Task Flags_method_that_calls_itself()
    {
        var ids = await IdsAsync(new DirectRecursionAnalyzer(), """
            class C
            {
                int Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);
            }
            """);
        Assert.Equal(["PT0001"], ids);
    }

    [Fact]
    public async Task Flags_recursive_local_function()
    {
        var ids = await IdsAsync(new DirectRecursionAnalyzer(), """
            class C
            {
                int M() { return Walk(3); int Walk(int n) => n == 0 ? 0 : Walk(n - 1); }
            }
            """);
        Assert.Equal(["PT0001"], ids);
    }

    [Fact]
    public async Task Ignores_calls_to_other_overloads()
    {
        var ids = await IdsAsync(new DirectRecursionAnalyzer(), """
            class C
            {
                int Sum(int a) => Sum(a, 0);
                int Sum(int a, int b) => a + b;
            }
            """);
        Assert.Empty(ids);
    }
}

public class UnboundedLoopTests
{
    [Theory]
    [InlineData("while (true) { break; }")]
    [InlineData("for (;;) { break; }")]
    [InlineData("do { break; } while (true);")]
    public async Task Flags_loops_without_a_bound(string loop)
    {
        var ids = await IdsAsync(new UnboundedLoopAnalyzer(), $"class C {{ void M() {{ {loop} }} }}");
        Assert.Equal(["PT0002"], ids);
    }

    [Fact]
    public async Task Ignores_bounded_loops()
    {
        var ids = await IdsAsync(new UnboundedLoopAnalyzer(), """
            class C { void M() { for (var i = 0; i < 20; i++) { } var n = 0; while (n < 5) n++; } }
            """);
        Assert.Empty(ids);
    }
}

public class UnboundedQueryTests
{
    private const string Header = """
        using System.Collections.Generic;
        using System.Linq;
        class Order { public int Total; }
        """;

    [Fact]
    public async Task Flags_queryable_materialized_without_take()
    {
        var ids = await IdsAsync(new UnboundedQueryAnalyzer(), Header + """
            class C { List<Order> M(IQueryable<Order> orders) => orders.Where(o => o.Total > 0).ToList(); }
            """);
        Assert.Equal(["PT0003"], ids);
    }

    [Fact]
    public async Task Ignores_queryable_with_take()
    {
        var ids = await IdsAsync(new UnboundedQueryAnalyzer(), Header + """
            class C { List<Order> M(IQueryable<Order> orders) => orders.Take(10).OrderBy(o => o.Total).ToList(); }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task Ignores_in_memory_enumerables()
    {
        var ids = await IdsAsync(new UnboundedQueryAnalyzer(), Header + """
            class C { List<Order> M(IEnumerable<Order> orders) => orders.Where(o => o.Total > 0).ToList(); }
            """);
        Assert.Empty(ids);
    }
}

public class MethodLengthTests
{
    [Fact]
    public async Task Flags_method_over_default_limit()
    {
        var body = string.Concat(Enumerable.Repeat("        x++;\n", MethodLengthAnalyzer.DefaultMaxLines));
        var ids = await IdsAsync(new MethodLengthAnalyzer(), $$"""
            class C
            {
                void Long()
                {
                    var x = 0;
            {{body}}
                }
            }
            """);
        Assert.Equal(["PT0004"], ids);
    }

    [Fact]
    public async Task Ignores_short_method()
    {
        var ids = await IdsAsync(new MethodLengthAnalyzer(), "class C { void M() { var x = 1; } }");
        Assert.Empty(ids);
    }
}

public class AssertionDensityTests
{
    [Fact]
    public async Task Flags_public_method_without_assertions()
    {
        var ids = await IdsAsync(new AssertionDensityAnalyzer(), """
            public class C
            {
                public int M(int a, int b)
                {
                    var x = a;
                    x += b;
                    x *= 2;
                    x -= 1;
                    x /= 3;
                    x += 7;
                    x %= 5;
                    return x;
                }
            }
            """);
        Assert.Equal(["PT0005"], ids);
    }

    [Fact]
    public async Task Counts_guards_throw_helpers_and_throws()
    {
        var ids = await IdsAsync(new AssertionDensityAnalyzer(), """
            using System;
            public class C
            {
                public int M(string s, int b)
                {
                    ArgumentNullException.ThrowIfNull(s);
                    if (b < 0) return 0;
                    var x = s.Length;
                    x += b;
                    x *= 2;
                    x -= 1;
                    x /= 3;
                    x += 7;
                    return x;
                }
            }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task Ignores_short_and_private_methods()
    {
        var ids = await IdsAsync(new AssertionDensityAnalyzer(), """
            public class C
            {
                public int Short(int a) => a + 1;
                private int Private(int a)
                {
                    var x = a;
                    x += 1;
                    x += 1;
                    x += 1;
                    x += 1;
                    x += 1;
                    x += 1;
                    return x;
                }
            }
            """);
        Assert.Empty(ids);
    }
}

public class CompilationSymbolTests
{
    [Fact]
    public async Task Flags_file_with_four_symbols()
    {
        var diagnostics = await RunAsync(new CompilationSymbolAnalyzer(), """
            class C
            {
                int M(int x)
                {
            #if A
                    x++;
            #endif
            #if B || C
                    x++;
            #elif D
                    x--;
            #endif
                    return x;
                }
            }
            """);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PT0008", diagnostic.Id);
        Assert.Contains("16 build variants", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Ignores_file_with_few_symbols()
    {
        var ids = await IdsAsync(new CompilationSymbolAnalyzer(), """
            class C
            {
            #if DEBUG
                int M() => 1;
            #else
                int M() => 2;
            #endif
            }
            """);
        Assert.Empty(ids);
    }
}

public class SingleImplementationInterfaceTests
{
    [Fact]
    public async Task Flags_interface_with_one_implementation()
    {
        var ids = await IdsAsync(new SingleImplementationInterfaceAnalyzer(), """
            interface IOrderService { void Close(); }
            class OrderService : IOrderService { public void Close() { } }
            """);
        Assert.Equal(["PT0009A"], ids);
    }

    [Fact]
    public async Task Ignores_interface_with_two_implementations()
    {
        var ids = await IdsAsync(new SingleImplementationInterfaceAnalyzer(), """
            interface IShape { double Area(); }
            class Square : IShape { public double Area() => 1; }
            class Circle : IShape { public double Area() => 3.14; }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task Treats_closed_generics_as_implementations_of_one_interface()
    {
        var ids = await IdsAsync(new SingleImplementationInterfaceAnalyzer(), """
            interface IHandler<T> { void Handle(T item); }
            class IntHandler : IHandler<int> { public void Handle(int item) { } }
            class TextHandler : IHandler<string> { public void Handle(string item) { } }
            """);
        Assert.Empty(ids);
    }
}

public class PassThroughMethodTests
{
    private const string Repo = """
        using System;
        using System.Threading.Tasks;
        class Repo
        {
            public Task<string> GetAsync(Guid id) => Task.FromResult("");
            public void Save(Guid id, string name) { }
        }
        """;

    [Theory]
    [InlineData("public Task<string> GetAsync(Guid id) => _repo.GetAsync(id);")]
    [InlineData("public async Task<string> GetAsync(Guid id) { return await _repo.GetAsync(id); }")]
    [InlineData("public void Save(Guid id, string name) { _repo.Save(id, name); }")]
    public async Task Flags_methods_that_only_forward(string method)
    {
        var ids = await IdsAsync(new PassThroughMethodAnalyzer(), Repo + $$"""
            class Service { private readonly Repo _repo = new(); {{method}} }
            """);
        Assert.Equal(["PT0009B"], ids);
    }

    [Theory]
    [InlineData("public void Save(Guid id, string name) { _repo.Save(id, name.Trim()); }")]
    [InlineData("public void Save(string name, Guid id) { _repo.Save(id, name); }")]
    [InlineData("public void Save(Guid id, string name) { Console.WriteLine(id); _repo.Save(id, name); }")]
    public async Task Ignores_methods_that_add_behavior(string method)
    {
        var ids = await IdsAsync(new PassThroughMethodAnalyzer(), Repo + $$"""
            class Service { private readonly Repo _repo = new(); {{method}} }
            """);
        Assert.Empty(ids);
    }
}
