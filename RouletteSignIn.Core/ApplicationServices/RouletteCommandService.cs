using System.Security.Cryptography;
using Tgs.ServiceUtility;

namespace Tgs.RouletteServices.ApplicationServices;

using GetRoulettePromotionService = IGetService<GetRoulettePromotionSettingsQuery, RoulettePromotion?>;

internal class RouletteCommandService : ICommandService<RouletteCommand>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly GetRoulettePromotionService _getRoulettePromotionService;

	public RouletteCommandService(
		ITgsRepository tgsRepository,
		GetRoulettePromotionService getRoulettePromotionService)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_getRoulettePromotionService = getRoulettePromotionService ?? throw new ArgumentNullException(nameof(getRoulettePromotionService));
	}

	public async ValueTask ExecuteAsync(RouletteCommand command, CancellationToken cancellationToken = default)
	{
		var currentRecord = await _tgsRepository.GetRouletteSignInRecordAsync(
			command.PromotionId,
			command.PlayerCode,
			command.Date,
			cancellationToken).ConfigureAwait(false);

		if (currentRecord is not null)
			return;

		var promotionSettings = await _getRoulettePromotionService.GetAsync(
			new GetRoulettePromotionSettingsQuery(command.PromotionId),
			cancellationToken).ConfigureAwait(false);

		if (promotionSettings is null)
			throw new InvalidOperationException($"Promotion {command.PromotionId} does not exist.");

		var itemCount = promotionSettings.CampaignDetail.Items.Count();

		if (itemCount == 0)
			throw new InvalidOperationException($"Promotion {command.PromotionId} does not have any items.");

		var itemIndex = RandomNumberGenerator.GetInt32(0, itemCount);

		await _tgsRepository.AddRouletteRecordAsync(
			command.PromotionId,
			command.PlayerCode,
			command.Date,
			itemIndex,
			cancellationToken).ConfigureAwait(false);
	}
}
