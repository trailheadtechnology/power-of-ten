using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PowerOfTen.Analyzers;

/// <summary>
/// PT0005: flags public methods of meaningful size with fewer than two assertions.
/// Counts as an assertion:
///   - throw statements and throw expressions
///   - ThrowIf* helpers (ArgumentNullException.ThrowIfNull, ...) and Debug/Trace.Assert
///   - top-level guard clauses: if (...) return ...; with no else
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AssertionDensityAnalyzer : DiagnosticAnalyzer
{
    public const int DefaultMinAssertions = 2;
    public const int DefaultMinLines = 10;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.LowAssertionDensity);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Check, SyntaxKind.MethodDeclaration);
    }

    private static void Check(SyntaxNodeAnalysisContext ctx)
    {
        var method = (MethodDeclarationSyntax)ctx.Node;
        if (method.Body is null || !method.Modifiers.Any(SyntaxKind.PublicKeyword))
            return;

        var tree = method.SyntaxTree;
        var minLines = Options.GetInt(ctx.Options, tree, Options.AssertionMinLines, DefaultMinLines);
        var span = method.Body.GetLocation().GetLineSpan();
        if (span.EndLinePosition.Line - span.StartLinePosition.Line + 1 < minLines)
            return;

        var minAssertions = Options.GetInt(ctx.Options, tree, Options.MinAssertions, DefaultMinAssertions);
        var count = CountAssertions(method.Body);
        if (count < minAssertions)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LowAssertionDensity, method.Identifier.GetLocation(),
                method.Identifier.Text, count, minAssertions));
        }
    }

    private static int CountAssertions(BlockSyntax body)
    {
        var throws = body.DescendantNodes().Count(n =>
            n.IsKind(SyntaxKind.ThrowStatement) || n.IsKind(SyntaxKind.ThrowExpression));

        var helpers = body.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Count(i => GetMethodName(i) is { } name && (name.StartsWith("ThrowIf") || name == "Assert"));

        var guards = body.Statements
            .OfType<IfStatementSyntax>()
            .Count(IsGuardClause);

        return throws + helpers + guards;
    }

    private static bool IsGuardClause(IfStatementSyntax ifStatement)
    {
        if (ifStatement.Else is not null)
            return false;

        return ifStatement.Statement switch
        {
            ReturnStatementSyntax => true,
            BlockSyntax { Statements.Count: 1 } block => block.Statements[0] is ReturnStatementSyntax,
            _ => false,
        };
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
        IdentifierNameSyntax id => id.Identifier.Text,
        _ => null,
    };
}
