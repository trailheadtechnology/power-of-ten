using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PowerOfTen.Analyzers;

/// <summary>
/// PT0002: flags while (true), do { } while (true), and for (;;).
/// It can't prove that other loops terminate; it only catches the ones that obviously don't try.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnboundedLoopAnalyzer : DiagnosticAnalyzer
{
    // The warnings this analyzer can raise
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.UnboundedLoop);

    public override void Initialize(AnalysisContext context)
    {
        // Skip generated code, run in parallel, and call Check on every while, do, and for loop
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Check,
            SyntaxKind.WhileStatement, SyntaxKind.DoStatement, SyntaxKind.ForStatement);
    }

    private static void Check(SyntaxNodeAnalysisContext ctx)
    {
        // Gets the loop's condition and keyword
        var (condition, keyword) = ctx.Node switch
        {
            WhileStatementSyntax w => (w.Condition, w.WhileKeyword),
            DoStatementSyntax d => (d.Condition, d.DoKeyword),
            ForStatementSyntax f => (f.Condition, f.ForKeyword),
            _ => (null, default(SyntaxToken)),
        };

        // for (;;) has no condition; while (true) has a literal true
        if (condition is null || condition.IsKind(SyntaxKind.TrueLiteralExpression))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.UnboundedLoop, keyword.GetLocation(), keyword.Text));
        }
    }
}
