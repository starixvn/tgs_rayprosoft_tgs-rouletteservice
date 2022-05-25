using System.Security.Cryptography;
using Tgs.ServiceUtility;

namespace Tgs.RouletteServices.ApplicationServices;

using GetRoulettePromotionService = IGetService<GetRoulettePromotionSettingsQuery, RoulettePromotion?>;

internal class RouletteCommandService : ICommandService<RouletteCommand>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly GetRoulettePromotionService _getRoulettePromotionService;
	private readonly IShuffleService _shuffleService;
	private readonly IStatisticsService _statisticsService;

	public RouletteCommandService(
		ITgsRepository tgsRepository,
		GetRoulettePromotionService getRoulettePromotionService,
		IShuffleService shuffleService,
		IStatisticsService statisticsService)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_getRoulettePromotionService = getRoulettePromotionService ?? throw new ArgumentNullException(nameof(getRoulettePromotionService));
		_shuffleService = shuffleService ?? throw new ArgumentNullException(nameof(shuffleService));
		_statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
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

		var rouletteItems = promotionSettings.CampaignDetail.Items.ToArray();

		if (promotionSettings.PromotionLimitAmount is not null
			&& await _statisticsService.CheckOverTotalDistributeAmountAsync(
				command.PromotionId,
				promotionSettings.PromotionLimitAmount.Value,
				cancellationToken).ConfigureAwait(false))
		{
			_ = await _statisticsService.IncrementTotalDistributeAmountAsync(
				command.PromotionId,
				null,
				rouletteItems[0].Bonus,
				rouletteItems[0].Bonus,
				cancellationToken).ConfigureAwait(false);

			await _tgsRepository.AddRouletteRecordAsync(
				command.PromotionId,
				command.PlayerCode,
				command.Date,
				0,
				cancellationToken).ConfigureAwait(false);

			return;
		}

		var deck = _shuffleService.Shuffle(command.PromotionId, promotionSettings.CampaignDetail.Items);

		var deckIndex = RandomNumberGenerator.GetInt32(0, deck.Length);

		var itemIndex = deck[deckIndex];

		if (!await _statisticsService.IncrementRouletteItemCountAsync(
			command.PromotionId,
			rouletteItems[itemIndex].Id,
			command.Date,
			rouletteItems[itemIndex].IsDefault || rouletteItems[itemIndex].DailyWinningLimit == 0
				? null
				: rouletteItems[itemIndex].DailyWinningLimit,
			cancellationToken).ConfigureAwait(false))
			itemIndex = 0;

		var distributeAmount = rouletteItems[itemIndex].Bonus;

		if (await _statisticsService.IncrementTotalDistributeAmountAsync(
			command.PromotionId,
			promotionSettings.PromotionLimitAmount,
			distributeAmount,
			rouletteItems[0].Bonus,
			cancellationToken).ConfigureAwait(false) is (false, _))
		{
			await _tgsRepository.AddRouletteRecordAsync(
				command.PromotionId,
				command.PlayerCode,
				command.Date,
				itemIndex,
				cancellationToken).ConfigureAwait(false);
		}
		else
		{
			await _tgsRepository.AddRouletteRecordAsync(
				command.PromotionId,
				command.PlayerCode,
				command.Date,
				0,
				cancellationToken).ConfigureAwait(false);
		}
	}
}
