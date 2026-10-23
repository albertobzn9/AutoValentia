using System.Text.RegularExpressions;
using System.Globalization;

namespace AutoValentia;

internal sealed record SessionIdentifier(decimal Day, int Rat)
{
    public string DayText => Day.ToString("0.##", CultureInfo.InvariantCulture);

    public string DaySheetName => $"d{DayText}";

    public string DisplayName => $"d{DayText}r{Rat}";
}

internal static partial class SessionFileName
{
    [GeneratedRegex(@"d(?<day>\d+(?:\.\d+)?)r(?<rat>\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();

    public static bool TryParse(string path, out SessionIdentifier? identifier)
    {
        var match = IdentifierPattern().Match(Path.GetFileNameWithoutExtension(path));
        if (!match.Success ||
            !decimal.TryParse(match.Groups["day"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var day) ||
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
