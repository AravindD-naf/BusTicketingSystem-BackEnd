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

        // Supports both "HH:mm:ss" (same day) and "d.HH:mm:ss" or "HH:mm:ss" with hours > 23
        // Try standard TimeSpan.Parse first — handles "36:15:00" natively
        if (TimeSpan.TryParse(value, out var result))
            return result;

        throw new JsonException($"Cannot convert \"{value}\" to TimeSpan.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        TimeSpan value,
        JsonSerializerOptions options)
    {
        // Write as "HH:mm:ss" for same-day, or "d.HH:mm:ss" for multi-day
        writer.WriteStringValue(value.ToString(@"hh\:mm\:ss"));
    }
}