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

		var totalDistributeAmount = await _statisticsService
			.GetTotalDistributeAmountAsync(command.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		var rouletteItems = promotionSettings.CampaignDetail.Items.ToArray();

		if (promotionSettings.PromotionLimitAmount is not null
			&& totalDistributeAmount >= promotionSettings.PromotionLimitAmount)
		{
			await _statisticsService.IncrementTotalDistributeAmountAsync(
				command.PromotionId,
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


		var distributeAmount = rouletteItems[itemIndex].Bonus;

		var resultAmount = await _statisticsService.IncrementTotalDistributeAmountAsync(
			command.PromotionId,
			distributeAmount,
			cancellationToken).ConfigureAwait(false);

		if (resultAmount > promotionSettings.PromotionLimitAmount
			&& resultAmount - distributeAmount >= promotionSettings.PromotionLimitAmount)
		{
			await _tgsRepository.AddRouletteRecordAsync(
				command.PromotionId,
				command.PlayerCode,
				command.Date,
				0,
				cancellationToken).ConfigureAwait(false);

			_ = await _statisticsService.DecreaseTotalDistributeAmountAsync(
				command.PromotionId,
				distributeAmount - rouletteItems[0].Bonus,
				cancellationToken).ConfigureAwait(false);

			return;
		}

		await _tgsRepository.AddRouletteRecordAsync(
			command.PromotionId,
			command.PlayerCode,
			command.Date,
			itemIndex,
			cancellationToken).ConfigureAwait(false);
	}
}
