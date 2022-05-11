using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.RouletteServices;
using Tgs.RouletteServices.ApplicationServices;
using Tgs.RouletteServices.Protos;
using Tgs.ServiceUtility;

namespace Tgs.RouletteService.MessageQueue;

public class RouletteQueryProcessor : IMessageProcessor
{
	private readonly IGetService<GetRouletteRecordQuery, RouletteSignInRecord?> _getRouletteRecordService;

	public RouletteQueryProcessor(
		IGetService<GetRouletteRecordQuery, RouletteSignInRecord?> getRouletteRecordService)
	{
		_getRouletteRecordService = getRouletteRecordService ?? throw new ArgumentNullException(nameof(getRouletteRecordService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RouletteRequest.Parser.ParseFrom(data.Span);

		var signRecord = await _getRouletteRecordService
			.GetAsync(new GetRouletteRecordQuery(
				request.PromotionId,
				request.PlayerCode,
				request.Date))
			.ConfigureAwait(false);

		return signRecord is null
			? null
			: new RouletteResponse
			{
				Id = signRecord.Id,
				ItemIndex = signRecord.ItemIndex
			}.ToByteArray();
	}
}
