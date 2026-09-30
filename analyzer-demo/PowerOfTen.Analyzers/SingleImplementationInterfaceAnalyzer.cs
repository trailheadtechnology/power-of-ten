using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PowerOfTen.Analyzers;

/// <summary>
/// PT0009A: flags interfaces declared in this project that exactly one type implements.
/// This needs the whole compilation, so in an IDE it only runs with full-solution analysis on.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleImplementationInterfaceAnalyzer : DiagnosticAnalyzer
{
    // The warnings this analyzer can raise
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.SingleImplementationInterface);

    public override void Initialize(AnalysisContext context)
    {
        // Skip generated code, run in parallel, and track types across the whole compilation
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            // Interfaces declared here, and the types that implement each one
            var interfaces = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);
            var implementations = new ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

            // Records every interface and what every type implements
            start.RegisterSymbolAction(ctx =>
            {
                var type = (INamedTypeSymbol)ctx.Symbol;
                if (type.TypeKind == TypeKind.Interface)
                {
                    interfaces.TryAdd(type, 0);
                    return;
                }

                foreach (var implemented in type.AllInterfaces)
                {
                    implementations
                        .GetOrAdd(implemented.OriginalDefinition, _ => new ConcurrentBag<INamedTypeSymbol>())
                        .Add(type);
                }
            }, SymbolKind.NamedType);

            // Once every type is seen, warns on interfaces with exactly one implementation
            start.RegisterCompilationEndAction(end =>
            {
                foreach (var iface in interfaces.Keys)
                {
                    if (!implementations.TryGetValue(iface, out var impls))
                        continue;

                    var distinct = impls.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToList();
                    if (distinct.Count != 1)
                        continue;

                    var location = iface.Locations.FirstOrDefault(l => l.IsInSource);
                    if (location is null)
                        continue;

                    end.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.SingleImplementationInterface, location, iface.Name, distinct[0].Name));
                }
            });
        });
    }
}
