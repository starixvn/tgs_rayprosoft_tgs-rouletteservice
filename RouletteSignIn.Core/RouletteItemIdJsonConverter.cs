using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.RouletteServices;

internal class RouletteItemJsonConverter : JsonConverter<RouletteItem>
{
	private static readonly JsonSerializerOptions _JsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
	};

	public override RouletteItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var item = JsonSerializer.Deserialize<RouletteItem>(ref reader, _JsonSerializerOptions);

		if (item is not null && item.Id == Guid.Empty)
			return item with { Id = Guid.NewGuid() };

		return item;
	}

	public override void Write(Utf8JsonWriter writer, RouletteItem value, JsonSerializerOptions options)
		=> JsonSerializer.Serialize(writer, value, _JsonSerializerOptions);
}
