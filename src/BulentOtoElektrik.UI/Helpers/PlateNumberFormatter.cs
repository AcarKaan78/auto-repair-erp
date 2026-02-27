using System.Text.RegularExpressions;

namespace BulentOtoElektrik.UI.Helpers;

public static class PlateNumberFormatter
{
    public static string FormatPlate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var cleaned = Regex.Replace(input.ToUpper().Trim(), @"\s+", "");

        // Turkish plate format: XX YYY NNN or XX YY NNNN
        var match = Regex.Match(cleaned, @"^(\d{2})([A-ZÇĞİÖŞÜ]{1,3})(\d{2,4})$");
        if (match.Success)
        {
            return $"{match.Groups[1].Value} {match.Groups[2].Value} {match.Groups[3].Value}";
        }

        return input.ToUpper().Trim();
    }

    public static bool ValidatePlate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        var cleaned = Regex.Replace(input.ToUpper().Trim(), @"\s+", "");
        return Regex.IsMatch(cleaned, @"^\d{2}[A-Z\u00c7\u011e\u0130\u00d6\u015e\u00dc]{1,3}\d{2,4}$");
    }
}
