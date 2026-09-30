using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PowerOfTen.Analyzers;

/// <summary>
/// PT0003: flags ToList()/ToArray()/ToListAsync() etc. on an IQueryable when nothing
/// earlier in the method chain calls Take().
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnboundedQueryAnalyzer : DiagnosticAnalyzer
{
    // Calls that run the query and load the results
    private static readonly ImmutableHashSet<string> MaterializingMethods = ImmutableHashSet.Create(
        "ToList", "ToListAsync",
        "ToArray", "ToArrayAsync",
        "ToDictionary", "ToDictionaryAsync",
        "ToHashSet", "ToHashSetAsync");

    // The warnings this analyzer can raise
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.UnboundedQuery);

    public override void Initialize(AnalysisContext context)
    {
        // Skip generated code, run in parallel, and call Check on every method call when IQueryable exists
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var queryable = start.Compilation.GetTypeByMetadataName("System.Linq.IQueryable");
            if (queryable is null)
                return;

            start.RegisterSyntaxNodeAction(ctx => Check(ctx, queryable), SyntaxKind.InvocationExpression);
        });
    }

    private static void Check(SyntaxNodeAnalysisContext ctx, INamedTypeSymbol queryable)
    {
        // Only calls like .ToList()
        var invocation = (InvocationExpressionSyntax)ctx.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var name = memberAccess.Name.Identifier.Text;
        if (!MaterializingMethods.Contains(name))
            return;

        // Only on an IQueryable with no Take() earlier in the chain
        var receiverType = ctx.SemanticModel.GetTypeInfo(memberAccess.Expression, ctx.CancellationToken).Type;
        if (receiverType is null || !IsQueryable(receiverType, queryable))
            return;

        if (ChainContainsTake(memberAccess.Expression))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            Descriptors.UnboundedQuery, memberAccess.Name.GetLocation(), name));
    }

    private static bool IsQueryable(ITypeSymbol type, INamedTypeSymbol queryable)
        => SymbolEqualityComparer.Default.Equals(type, queryable)
           || type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, queryable));

    // Walks _db.Orders.Where(...).Take(n).OrderBy(...) back toward the source looking for Take
    private static bool ChainContainsTake(ExpressionSyntax expression)
    {
        while (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax link })
        {
            if (link.Name.Identifier.Text == "Take")
                return true;
            expression = link.Expression;
        }
        return false;
    }
}
