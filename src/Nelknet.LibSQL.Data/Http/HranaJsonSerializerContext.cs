#nullable disable warnings

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nelknet.LibSQL.Data.Http;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(HranaBatchRequest))]
[JsonSerializable(typeof(HranaBatchResponse))]
internal sealed partial class HranaJsonSerializerContext : JsonSerializerContext;

internal sealed class HranaValueJsonConverter : JsonConverter<HranaValue>
{
    public override HranaValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected Hrana value object.");

        var value = new HranaValue();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return value;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected Hrana value property.");

            var propertyName = reader.GetString();
            if (!reader.Read())
                throw new JsonException("Unexpected end of Hrana value.");

            switch (propertyName)
            {
                case "type":
                    value.Type = reader.GetString();
                    break;
                case "value":
                    value.Value = JsonElement.ParseValue(ref reader);
                    break;
                case "base64":
                    value.Base64 = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Unexpected end of Hrana value.");
    }

    public override void Write(Utf8JsonWriter writer, HranaValue value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WriteString("type", value.Type);

        if (value.Value != null)
        {
            writer.WritePropertyName("value");
            WriteValue(writer, value.Value);
        }

        if (value.Base64 != null)
            writer.WriteString("base64", value.Base64);

        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, object value)
    {
        switch (value)
        {
            case JsonElement element:
                element.WriteTo(writer);
                break;
            case string stringValue:
                writer.WriteStringValue(stringValue);
                break;
            case bool boolValue:
                writer.WriteBooleanValue(boolValue);
                break;
            case byte byteValue:
                writer.WriteNumberValue(byteValue);
                break;
            case sbyte sbyteValue:
                writer.WriteNumberValue(sbyteValue);
                break;
            case short shortValue:
                writer.WriteNumberValue(shortValue);
                break;
            case ushort ushortValue:
                writer.WriteNumberValue(ushortValue);
                break;
            case int intValue:
                writer.WriteNumberValue(intValue);
                break;
            case uint uintValue:
                writer.WriteNumberValue(uintValue);
                break;
            case long longValue:
                writer.WriteNumberValue(longValue);
                break;
            case ulong ulongValue:
                writer.WriteNumberValue(ulongValue);
                break;
            case float floatValue:
                writer.WriteNumberValue(floatValue);
                break;
            case double doubleValue:
                writer.WriteNumberValue(doubleValue);
                break;
            case decimal decimalValue:
                writer.WriteNumberValue(decimalValue);
                break;
            default:
                writer.WriteStringValue(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                break;
        }
    }
}
