using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.RouletteServices.Protos;

namespace Tgs.RouletteServices.Clients;

internal class RouletteSignInClient : IRouletteSignInClient
{
	private readonly IMessageSender _messageSender;

	public RouletteSignInClient(IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask<RouletteSignInRecord> RouletteAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date,
		CancellationToken cancellationToken = default)
	{
		var request = new RouletteRequest
		{
			PromotionId = promotionId,
			PlayerCode = playerCode,
			Date = date
		};

		var responseData = await _messageSender!
			.RequestAsync("tgs.roulette.daily", request.ToByteArray(), cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var response = RouletteResponse.Parser.ParseFrom(responseData.Span);

		return new RouletteSignInRecord(response.Id, response.ItemIndex);
	}

	public async ValueTask<RouletteSignInRecord?> GetRouletteRecordAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date,
		CancellationToken cancellationToken = default)
	{
		var request = new RouletteRequest
		{
			PromotionId = promotionId,
			PlayerCode = playerCode,
			Date = date
		};

		var responseData = await _messageSender!
			.RequestAsync("tgs.roulette.daily.query", request.ToByteArray(), cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var response = responseData.Length > 0
			? RouletteResponse.Parser.ParseFrom(responseData.Span)
			: null;

		return response is null
			? null
			: new RouletteSignInRecord(response.Id, response.ItemIndex);
	}
}
