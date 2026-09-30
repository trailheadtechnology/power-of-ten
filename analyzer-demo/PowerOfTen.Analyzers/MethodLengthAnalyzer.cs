using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PowerOfTen.Analyzers;

/// <summary>
/// PT0004: flags methods, constructors, and local functions longer than one printed page.
/// Holzmann's rule of thumb is about 60 lines; override with power_of_ten.max_method_lines.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodLengthAnalyzer : DiagnosticAnalyzer
{
    public const int DefaultMaxLines = 60;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.MethodTooLong);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Check,
            SyntaxKind.MethodDeclaration, SyntaxKind.ConstructorDeclaration, SyntaxKind.LocalFunctionStatement);
    }

    private static void Check(SyntaxNodeAnalysisContext ctx)
    {
        var (identifier, body) = ctx.Node switch
        {
            MethodDeclarationSyntax m => (m.Identifier, (SyntaxNode?)m.Body ?? m.ExpressionBody),
            ConstructorDeclarationSyntax c => (c.Identifier, (SyntaxNode?)c.Body ?? c.ExpressionBody),
            LocalFunctionStatementSyntax l => (l.Identifier, (SyntaxNode?)l.Body ?? l.ExpressionBody),
            _ => (default(SyntaxToken), null),
        };
        if (body is null)
            return;

        var span = ctx.Node.GetLocation().GetLineSpan();
        var lines = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
        var max = Options.GetInt(ctx.Options, ctx.Node.SyntaxTree, Options.MaxMethodLines, DefaultMaxLines);

        if (lines > max)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.MethodTooLong, identifier.GetLocation(), identifier.Text, lines, max));
        }
    }
}
