using System.Text.RegularExpressions;

namespace AutoValentia;

internal sealed record SessionIdentifier(int Day, int Rat)
{
    public string DaySheetName => $"d{Day}";

    public string DisplayName => $"d{Day}r{Rat}";
}

internal static partial class SessionFileName
{
    [GeneratedRegex(@"d(?<day>\d+)r(?<rat>\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();

    public static bool TryParse(string path, out SessionIdentifier? identifier)
    {
        var match = IdentifierPattern().Match(Path.GetFileNameWithoutExtension(path));
        if (!match.Success ||
            !int.TryParse(match.Groups["day"].Value, out var day) ||
            !int.TryParse(match.Groups["rat"].Value, out var rat) ||
            day < 1 || rat < 1)
        {
            identifier = null;
            return false;
        }

        identifier = new SessionIdentifier(day, rat);
        return true;
    }

    public static string GetExperimentName(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var match = IdentifierPattern().Match(fileName);
        var prefix = match.Success ? fileName[..match.Index].TrimEnd('_', '-') : fileName;
        return string.IsNullOrWhiteSpace(prefix) ? "AutoValentia" : prefix;
    }
}
