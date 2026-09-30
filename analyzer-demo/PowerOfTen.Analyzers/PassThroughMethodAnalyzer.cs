using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PowerOfTen.Analyzers;

/// <summary>
/// PT0009B: flags a method whose whole body forwards its own parameters, unchanged and in order,
/// to one other method, e.g. <c>public Task&lt;Order&gt; GetAsync(Guid id) =&gt; _repo.GetAsync(id);</c>
/// Overrides are skipped because the language requires them.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PassThroughMethodAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.PassThroughMethod);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Check, SyntaxKind.MethodDeclaration);
    }

    private static void Check(SyntaxNodeAnalysisContext ctx)
    {
        var method = (MethodDeclarationSyntax)ctx.Node;
        if (method.Modifiers.Any(SyntaxKind.OverrideKeyword))
            return;

        var parameters = method.ParameterList.Parameters;
        if (parameters.Count == 0)
            return;

        if (GetSingleExpression(method) is not InvocationExpressionSyntax invocation)
            return;

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count != parameters.Count)
            return;

        for (var i = 0; i < arguments.Count; i++)
        {
            if (arguments[i].NameColon is not null
                || arguments[i].Expression is not IdentifierNameSyntax id
                || id.Identifier.Text != parameters[i].Identifier.Text)
            {
                return;
            }
        }

        ctx.ReportDiagnostic(Diagnostic.Create(
            Descriptors.PassThroughMethod, method.Identifier.GetLocation(),
            method.Identifier.Text, invocation.Expression.ToString()));
    }

    // The one expression a method evaluates, for "=> x", "{ return x; }", or "{ x; }", with await unwrapped
    private static ExpressionSyntax? GetSingleExpression(MethodDeclarationSyntax method)
    {
        var expression = method.ExpressionBody?.Expression ?? (method.Body?.Statements is { Count: 1 } statements
            ? statements[0] switch
            {
                ReturnStatementSyntax r => r.Expression,
                ExpressionStatementSyntax e => e.Expression,
                _ => null,
            }
            : null);

        return expression is AwaitExpressionSyntax await ? await.Expression : expression;
    }
}
