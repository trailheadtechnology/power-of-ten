using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PowerOfTen.Analyzers;

/// <summary>
/// PT0001: flags a method or local function that calls itself.
/// Indirect recursion (A calls B calls A) needs a call graph and is out of scope.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DirectRecursionAnalyzer : DiagnosticAnalyzer
{
    // The warnings this analyzer can raise
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.DirectRecursion);

    public override void Initialize(AnalysisContext context)
    {
        // Skip generated code, run in parallel, and call Check on every method call
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Check, SyntaxKind.InvocationExpression);
    }

    private static void Check(SyntaxNodeAnalysisContext ctx)
    {
        // Resolves the method being called
        var invocation = (InvocationExpressionSyntax)ctx.Node;
        if (ctx.SemanticModel.GetSymbolInfo(invocation, ctx.CancellationToken).Symbol is not IMethodSymbol target)
            return;

        // GetEnclosingSymbol returns the innermost method, including local functions and lambdas
        var enclosing = ctx.SemanticModel.GetEnclosingSymbol(invocation.SpanStart, ctx.CancellationToken) as IMethodSymbol;
        if (enclosing is null)
            return;

        // Warns if it's the method we're already in
        var callee = target.ReducedFrom ?? target;
        if (SymbolEqualityComparer.Default.Equals(callee.OriginalDefinition, enclosing.OriginalDefinition))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.DirectRecursion, invocation.GetLocation(), enclosing.Name));
        }
    }
}
