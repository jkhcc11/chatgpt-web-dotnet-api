using System.Text.Json;
using System.Text.Json.Serialization;

namespace GptWeb.DotNet.Api.JsonConvert
{
    public class LongConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetInt64();

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }

}
