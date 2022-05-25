using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.RouletteServices;
using Tgs.RouletteServices.ApplicationServices;
using Tgs.RouletteServices.Protos;
using Tgs.ServiceUtility;

namespace Tgs.RouletteService.MessageQueue;

public class RouletteSignInProcessor : IMessageProcessor
{
	private readonly ICommandService<RouletteCommand> _rouletteService;
	private readonly IGetService<GetRouletteRecordQuery, RouletteSignInRecord?> _getRouletteRecordService;

	public RouletteSignInProcessor(
		ICommandService<RouletteCommand> rouletteService,
		IGetService<GetRouletteRecordQuery, RouletteSignInRecord?> getRouletteRecordService)
	{
		_rouletteService = rouletteService ?? throw new ArgumentNullException(nameof(rouletteService));
		_getRouletteRecordService = getRouletteRecordService ?? throw new ArgumentNullException(nameof(getRouletteRecordService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RouletteRequest.Parser.ParseFrom(data.Span);

		var command = new RouletteCommand(
			request.PromotionId,
			request.PlayerCode,
			request.Date);

		await _rouletteService
			.ExecuteAsync(command, cancellationToken)
			.ConfigureAwait(false);

		var signRecord = await _getRouletteRecordService
			.GetAsync(new GetRouletteRecordQuery(
				command.PromotionId,
				command.PlayerCode,
				command.Date), cancellationToken)
			.ConfigureAwait(false);

		return new RouletteResponse
		{
			Id = signRecord!.Id,
			ItemIndex = signRecord!.ItemIndex
		}.ToByteArray();
	}
}
