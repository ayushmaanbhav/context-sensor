namespace ContextSensor.Core.Output;

/// <summary>
/// Utility class for formatting attribute values for output
/// </summary>
public static class ValueFormatter
{
    /// <summary>
    /// Formats a value based on its datatype
    /// </summary>
    public static string FormatValue(object? value, string datatype)
    {
        if (value == null) return string.Empty;

        return datatype.ToLowerInvariant() switch
        {
            "integer" or "long" => value.ToString() ?? string.Empty,
            "double" => value.ToString() ?? string.Empty,
            "boolean" => value.ToString()?.ToLower() ?? string.Empty,
            "datetimeutc" => value is DateTime dtUtc
                ? dtUtc.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff")
                : value.ToString() ?? string.Empty,
            "datetimewithtz" => value is DateTime dtTz
                ? dtTz.ToString("yyyy-MM-dd HH:mm:ss.fffzzz")
                : value.ToString() ?? string.Empty,
            "timespan" => value is TimeSpan ts
                ? ts.ToString(@"d\.hh\:mm\:ss")
                : value.ToString() ?? string.Empty,
            "string" => $"\"{value.ToString()}\"" ?? string.Empty,
            _ => throw new ArgumentException($"Unsupported datatype: {datatype}", nameof(datatype))
        };
    }
}
