using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;
using Tgs.RouletteServices;

namespace Tgs.RouletteService.MessageQueue;

public class RouletteSettingProcessor : IMessageProcessor
{
	private readonly RouletteSerializer _rouletteSerializer;

	public RouletteSettingProcessor(RouletteSerializer rouletteSerializer)
	{
		_rouletteSerializer = rouletteSerializer ?? throw new ArgumentNullException(nameof(rouletteSerializer));
	}

	public ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = CampaignDetailRequest.Parser.ParseFrom(data.Span);

		var settings = _rouletteSerializer.Deserialize<RouletteSettings>(request.Settings);

		if (settings is null)
			throw new InvalidOperationException("Settings is null");

		var response = new CampaignDetailResponse
		{
			CampaignDetail = _rouletteSerializer.Serialize(settings)
		};

		return ValueTask.FromResult((ReadOnlyMemory<byte>)response.ToByteArray());
	}
}
