# Power of Ten Analyzers

Roslyn analyzers that enforce NASA/JPL's Power of Ten rules in C#, plus a sample app that breaks each one.

| ID      | Rule | Flags                                                                  | Default |
|---------|------|------------------------------------------------------------------------|---------|
| PT0001  | 1    | A method or local function that calls itself                           | Warning |
| PT0002  | 2    | `while (true)`, `for (;;)`, `do { } while (true)`                      | Warning |
| PT0003  | 3    | `ToList()`/`ToListAsync()`/etc. on an `IQueryable` with no `Take()`    | Warning |
| PT0004  | 4    | Methods longer than `power_of_ten.max_method_lines` (default 60)       | Warning |
| PT0005  | 5    | Public methods of 10+ lines with fewer than 2 assertions               | Warning |
| CA1806  | 7    | Ignored `TryParse` results (built into .NET, no custom analyzer needed)| Info    |
| PT0008  | 8    | Files whose `#if` directives use more than 3 symbols                   | Warning |
| PT0009A | 9    | Interfaces with exactly one implementation                             | Info    |
| PT0009B | 9    | Methods that only forward their parameters to another method           | Info    |

What counts as an assertion for PT0005: `throw`, `ThrowIf*` helpers, `Debug.Assert`/`Trace.Assert`,
and top-level guard clauses such as `if (x is null) return false;`.

## Layout

- `PowerOfTen.Analyzers/`: the analyzers (netstandard2.0)
- `PowerOfTen.Analyzers.Tests/`: xUnit tests (`dotnet test`)
- `SampleApp/`: one file per rule, with ❌ code that trips the analyzer next to ✅ code that doesn't.
  `SampleApp/.editorconfig` holds the thresholds and severities.

## Seeing the diagnostics

- **Command line:** `dotnet build SampleApp` prints 12 warnings.
- **VS Code:** open the `analyzer-demo` folder with the C# Dev Kit extension installed. Diagnostics
  appear as squiggles and in the Problems panel (View → Problems). `.vscode/settings.json` turns on
  full-solution analysis, which PT0009A needs because it looks at the whole project.
- After changing analyzer code, run **Developer: Reload Window** so VS Code loads the new DLL.

## Demo script

1. `dotnet build SampleApp`: walk through the warnings by rule number.
2. Open `Rule2_UnboundedLoops.cs` and show the squiggle on `while (true)`.
3. Open `PowerOfTen.Analyzers/UnboundedLoopAnalyzer.cs`: about 30 lines for the whole rule.
4. In `SampleApp/.editorconfig`, change `power_of_ten.max_method_lines` from 40 to 60, and PT0004 goes away.
   Every team gets to set its own level.
5. Uncomment `dotnet_diagnostic.PT0002.severity = error`, or set `TreatWarningsAsErrors` to `true`
   in `SampleApp.csproj`, and build again. The build now fails.
6. Open `Rule9_Indirection.cs` and ask the room: does `ICustomerService` deserve to exist?
