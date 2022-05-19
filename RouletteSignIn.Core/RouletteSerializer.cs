using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.RouletteServices;

public class RouletteSerializer
{
	private readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
	};

	public T? Deserialize<T>(string json)
		=> JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);

	public string Serialize<T>(T data)
		=> JsonSerializer.Serialize(data, _jsonSerializerOptions);
}
