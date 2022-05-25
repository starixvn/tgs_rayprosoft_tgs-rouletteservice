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

		//if (settings.Items.Count() == 0)
		//	throw new InvalidOperationException("Settings has no items");

		var defaultItem = settings.Items.FirstOrDefault(item => item.IsDefault)
			?? settings.Items.First();

		var defaultItemWinningProbability = 1M - settings.Items.Where(
			item => item.Id != defaultItem.Id).Sum(x => x.WinningProbability);

		if (defaultItemWinningProbability < 0M)
			throw new InvalidOperationException("Summary winning probability is grant to 1");

		var orderedItems = new[] { defaultItem with { IsDefault = true, WinningProbability = defaultItemWinningProbability } }
			.Union(settings.Items
				.Where(item => item.Id != defaultItem.Id)
				.Select(item => item with { IsDefault = false }));

		var response = new CampaignDetailResponse
		{
			CampaignDetail = _rouletteSerializer.Serialize(settings with { Items = orderedItems })
		};

		return ValueTask.FromResult((ReadOnlyMemory<byte>)response.ToByteArray());
	}
}
