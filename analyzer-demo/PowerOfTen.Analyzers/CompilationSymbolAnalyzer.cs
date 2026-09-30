using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PowerOfTen.Analyzers;

/// <summary>
/// PT0008: flags a file whose #if/#elif directives use more distinct symbols than allowed.
/// Each symbol doubles the number of build variants: n symbols means 2^n builds to test.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CompilationSymbolAnalyzer : DiagnosticAnalyzer
{
    public const int DefaultMaxSymbols = 3;

    // The warnings this analyzer can raise
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.TooManyCompilationSymbols);

    public override void Initialize(AnalysisContext context)
    {
        // Skip generated code, run in parallel, and call Check once per file
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(Check);
    }

    private static void Check(SyntaxTreeAnalysisContext ctx)
    {
        // Reads every #if and #elif directive in the file
        var root = ctx.Tree.GetRoot(ctx.CancellationToken);
        var conditions = root.DescendantTrivia()
            .Where(t => t.IsKind(SyntaxKind.IfDirectiveTrivia) || t.IsKind(SyntaxKind.ElifDirectiveTrivia))
            .Select(t => t.GetStructure())
            .OfType<ConditionalDirectiveTriviaSyntax>()
            .ToList();
        if (conditions.Count == 0)
            return;

        // Collects the distinct symbol names they test
        var symbols = conditions
            .SelectMany(d => d.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            .Select(id => id.Identifier.Text)
            .Distinct()
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        // Warns when there are more symbols than allowed
        var max = Options.GetInt(ctx.Options, ctx.Tree, Options.MaxCompilationSymbols, DefaultMaxSymbols);
        if (symbols.Count <= max)
            return;

        var variants = symbols.Count >= 62 ? long.MaxValue : 1L << symbols.Count;
        ctx.ReportDiagnostic(Diagnostic.Create(
            Descriptors.TooManyCompilationSymbols, conditions[0].GetLocation(),
            symbols.Count, string.Join(", ", symbols), variants.ToString("N0")));
    }
}
