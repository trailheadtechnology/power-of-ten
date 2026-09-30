using Microsoft.CodeAnalysis;

namespace PowerOfTen.Analyzers;

/// <summary>
/// Diagnostic IDs follow the Power of Ten rule numbers: PT0002 is Rule 2, and so on.
/// </summary>
internal static class Descriptors
{
    private const string Category = "PowerOfTen";

    // One descriptor per warning: ID, message, and severity
    public static readonly DiagnosticDescriptor DirectRecursion = new(
        id: "PT0001",
        title: "Avoid recursion",
        messageFormat: "'{0}' calls itself recursively; use an explicit loop so depth is bounded (Power of Ten, Rule 1)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Recursion makes call-stack depth unknowable at compile time. Replace it with an explicit loop and stack.");

    public static readonly DiagnosticDescriptor UnboundedLoop = new(
        id: "PT0002",
        title: "Loop has no fixed upper bound",
        messageFormat: "'{0}' loop has no fixed upper bound (Power of Ten, Rule 2)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Loops like while (true) and for (;;) can run forever. Give every loop a bound it can prove.");

    public static readonly DiagnosticDescriptor UnboundedQuery = new(
        id: "PT0003",
        title: "Query materializes without an upper bound",
        messageFormat: "'{0}' loads every matching row; add .Take(n) before materializing (Power of Ten, Rule 3)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Materializing an IQueryable without Take() makes memory use depend on how much data exists.");

    public static readonly DiagnosticDescriptor MethodTooLong = new(
        id: "PT0004",
        title: "Method is longer than a printed page",
        messageFormat: "'{0}' is {1} lines long; the limit is {2} (Power of Ten, Rule 4)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Long methods are hard to review and test. Set the limit with 'power_of_ten.max_method_lines' in .editorconfig.");

    public static readonly DiagnosticDescriptor LowAssertionDensity = new(
        id: "PT0005",
        title: "Too few assertions",
        messageFormat: "'{0}' has {1} assertion(s); expected at least {2} (Power of Ten, Rule 5)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Public methods should check their preconditions with guard clauses, ThrowIf helpers, or Debug.Assert.");

    public static readonly DiagnosticDescriptor TooManyCompilationSymbols = new(
        id: "PT0008",
        title: "Too many conditional compilation symbols",
        messageFormat: "File uses {0} conditional compilation symbols ({1}), which makes {2} build variants to test (Power of Ten, Rule 8)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every #if symbol doubles the number of builds. Prefer runtime feature flags.");

    public static readonly DiagnosticDescriptor SingleImplementationInterface = new(
        id: "PT0009A",
        title: "Interface has only one implementation",
        messageFormat: "Interface '{0}' has a single implementation ('{1}'); is the indirection earning its keep? (Power of Ten, Rule 9)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "An interface with one implementation adds indirection without adding flexibility.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor PassThroughMethod = new(
        id: "PT0009B",
        title: "Method only forwards its arguments",
        messageFormat: "'{0}' only forwards its arguments to '{1}' (Power of Ten, Rule 9)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A method that only forwards its arguments is a layer of indirection with no behavior of its own.");
}
