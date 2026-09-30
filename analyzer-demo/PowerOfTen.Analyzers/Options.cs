using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PowerOfTen.Analyzers;

/// <summary>
/// Reads integer thresholds from .editorconfig, e.g. <c>power_of_ten.max_method_lines = 40</c>.
/// </summary>
internal static class Options
{
    public const string MaxMethodLines = "power_of_ten.max_method_lines";
    public const string MinAssertions = "power_of_ten.min_assertions";
    public const string AssertionMinLines = "power_of_ten.assertion_min_lines";
    public const string MaxCompilationSymbols = "power_of_ten.max_compilation_symbols";

    public static int GetInt(AnalyzerOptions options, SyntaxTree tree, string key, int defaultValue)
    {
        var config = options.AnalyzerConfigOptionsProvider.GetOptions(tree);
        return config.TryGetValue(key, out var raw) && int.TryParse(raw, out var value) && value > 0
            ? value
            : defaultValue;
    }
}
