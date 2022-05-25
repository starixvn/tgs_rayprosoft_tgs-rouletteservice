using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.RouletteServices.ApplicationServices;

internal class GetRoulettePromotionSettingsQueryService : IGetService<GetRoulettePromotionSettingsQuery, RoulettePromotion?>
{
	private readonly IPromotionServiceClient _promotionServiceClient;

	public GetRoulettePromotionSettingsQueryService(IPromotionServiceClient promotionServiceClient)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
	}

	public async ValueTask<RoulettePromotion?> GetAsync(GetRoulettePromotionSettingsQuery query, CancellationToken cancellationToken = default)
	{
		var mainInfo = await _promotionServiceClient
			.GetPromotionMainInfoAsync(query.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		if (mainInfo is null)
			return null;

		if (mainInfo.PromotionType != "Roulette")
			throw new InvalidOperationException($"Promotion with id {query.PromotionId} is not a roulette promotion");

		var settings = await _promotionServiceClient
			.GetPromotionRuleAsync<RouletteSettings>(query.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		return settings is null
			? throw new InvalidOperationException("Roulette settings is null")
			: new RoulettePromotion(
			mainInfo.Id,
			mainInfo.PromotionLimitAmount,
			settings);
	}
}
