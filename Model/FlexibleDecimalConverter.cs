using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imdeliceapp.Models;

public class FlexibleDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.TryGetDecimal(out var value) ? value : Convert.ToDecimal(reader.GetDouble()),
            JsonTokenType.String => decimal.TryParse(reader.GetString(), out var parsed) ? parsed : 0m,
            _ => 0m
        };
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

public class FlexibleNullableDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.TryGetDecimal(out var value) ? value : Convert.ToDecimal(reader.GetDouble()),
            JsonTokenType.String => decimal.TryParse(reader.GetString(), out var parsed) ? parsed : (decimal?)null,
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
