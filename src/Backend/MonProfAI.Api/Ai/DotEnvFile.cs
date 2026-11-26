namespace MonProfAI.Api.Ai;

/// <summary>
/// Reads one value from the repository's <c>.env</c> file for local runs (Docker Compose already injects it).
/// Only the requested key is read on purpose: the rest of <c>.env</c> targets Docker (e.g. Host=postgres)
/// and would break a local <c>dotnet run</c> if loaded wholesale.
/// </summary>
public static class DotEnvFile
{
    public static string? ReadValue(string startDirectory, string key)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, ".env");
            if (File.Exists(path))
                return ParseValue(File.ReadLines(path), key);
        }

        return null;
    }

    public static string? ParseValue(IEnumerable<string> lines, string key)
    {
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (line.StartsWith("export "))
                line = line["export ".Length..].TrimStart();

            var separator = line.IndexOf('=');
            if (separator <= 0 || line[..separator].Trim() != key)
                continue;

            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
                value = value[1..^1];
            return value.Length == 0 ? null : value;
        }

        return null;
    }
}
