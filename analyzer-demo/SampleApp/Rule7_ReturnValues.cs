namespace SampleApp.Rule7;

// Rule 7 needs no custom analyzer: .NET's built-in CA1806 already catches it.
// SampleApp/.editorconfig raises CA1806 to a warning.
public static class PortParser
{
    // ❌ CA1806: TryParse's return value is ignored, so a bad input silently becomes port 0
    public static int ReadPort(string input)
    {
        int.TryParse(input, out var port);
        return port;
    }

    // ✅ the success flag is checked
    public static int ReadPortChecked(string input)
    {
        if (!int.TryParse(input, out var port))
            throw new ArgumentException("Invalid port.", nameof(input));
        return port;
    }

    // ✅ ignoring the result on purpose, and saying so with a discard
    public static int ReadPortOrDefault(string input)
    {
        _ = int.TryParse(input, out var port);
        return port == 0 ? 443 : port;
    }
}
