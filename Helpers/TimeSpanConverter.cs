using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class TimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException("Invalid TimeSpan value.");

        // Handle "H:mm:ss", "HH:mm:ss", "HHH:mm:ss" — any number of hours
        // TimeSpan.Parse natively supports hours > 23 (e.g. "36:15:00" = 1 day 12h 15m)
        if (TimeSpan.TryParse(value, out var result))
            return result;

        throw new JsonException($"Cannot convert \"{value}\" to TimeSpan.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        TimeSpan value,
        JsonSerializerOptions options)
    {
        // Always write as total hours:mm:ss to preserve overnight values
        var totalHours = (int)value.TotalHours;
        var minutes = value.Minutes;
        var seconds = value.Seconds;
        writer.WriteStringValue(
            $"{totalHours:D2}:{minutes:D2}:{seconds:D2}");
    }
}