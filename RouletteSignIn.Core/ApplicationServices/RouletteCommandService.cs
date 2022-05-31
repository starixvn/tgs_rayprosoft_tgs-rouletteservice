using System.Security.Cryptography;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.RouletteServices.ApplicationServices;

using DistributeService = ICommandService<DistributeBonusCommand>;
using GetRoulettePromotionService = IGetService<GetRoulettePromotionSettingsQuery, RoulettePromotion?>;

internal class RouletteCommandService : ICommandService<RouletteCommand>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly GetRoulettePromotionService _getRoulettePromotionService;
	private readonly IShuffleService _shuffleService;
	private readonly IStatisticsService _statisticsService;
	private readonly DistributeService _distributeService;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public RouletteCommandService(
		ITgsRepository tgsRepository,
		GetRoulettePromotionService getRoulettePromotionService,
		IShuffleService shuffleService,
		IStatisticsService statisticsService,
		DistributeService distributeService,
		IPromotionServiceClient promotionServiceClient,
		IPlayerServiceClient playerServiceClient)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_getRoulettePromotionService = getRoulettePromotionService ?? throw new ArgumentNullException(nameof(getRoulettePromotionService));
		_shuffleService = shuffleService ?? throw new ArgumentNullException(nameof(shuffleService));
		_statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
		_distributeService = distributeService ?? throw new ArgumentNullException(nameof(distributeService));
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
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

		var player = await _playerServiceClient
			.GetPlayerInfoAsync(command.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (player is null)
			throw new InvalidOperationException($"Can't find player with code {command.PlayerCode}");

		var promotionSettings = await _getRoulettePromotionService.GetAsync(
			new GetRoulettePromotionSettingsQuery(command.PromotionId),
			cancellationToken).ConfigureAwait(false);

		if (promotionSettings is null)
			throw new InvalidOperationException($"Promotion {command.PromotionId} does not exist.");

		var itemCount = promotionSettings.CampaignDetail.Items.Count();

		if (itemCount == 0)
			throw new InvalidOperationException($"Promotion {command.PromotionId} does not have any items.");

		var rouletteItems = promotionSettings.CampaignDetail.Items.ToArray();

		var (recordId, totalAmount, bonus, bucket) = await ProcessRouletteAsync(
			command,
			promotionSettings,
			rouletteItems,
			player.BrandId,
			cancellationToken).ConfigureAwait(false);

		_ = Task.Run(async () =>
		{
			await _promotionServiceClient.UpdatePromotionUsedAmountAsync(
				command.PromotionId,
				totalAmount,
				recordId,
				default).ConfigureAwait(false);

			await _distributeService.ExecuteAsync(new DistributeBonusCommand(
				command.PromotionId,
				command.PlayerCode,
				command.Date,
				bonus,
				bucket), default).ConfigureAwait(false);
		});
	}

	private async Task<(Guid RecordId, decimal TotalAmount, decimal Bonus, decimal Bucket)> ProcessRouletteAsync(
		RouletteCommand command,
		RoulettePromotion promotionSettings,
		RouletteItem[] rouletteItems,
		Guid brandId,
		CancellationToken cancellationToken)
	{
		if (promotionSettings.PromotionLimitAmount is not null
			&& await _statisticsService.CheckOverTotalDistributeAmountAsync(
				command.PromotionId,
				promotionSettings.PromotionLimitAmount.Value,
				cancellationToken).ConfigureAwait(false))
		{
			return await ProcessRouletteResultAsync(
				command,
				promotionSettings,
				rouletteItems,
				brandId,
				0,
				cancellationToken).ConfigureAwait(false);
		}
		else
		{
			var itemIndex = await PlayRouletteAsync(
				command,
				rouletteItems,
				cancellationToken).ConfigureAwait(false);

			return await ProcessRouletteResultAsync(
				command,
				promotionSettings,
				rouletteItems,
				brandId,
				itemIndex,
				cancellationToken).ConfigureAwait(false);
		}
	}

	private async Task<(Guid RecordId, decimal TotalAmount, decimal Bonus, decimal Bucket)> ProcessRouletteResultAsync(
		RouletteCommand command,
		RoulettePromotion promotionSettings,
		RouletteItem[] rouletteItems,
		Guid brandId,
		int itemIndex,
		CancellationToken cancellationToken)
	{
		var distributeAmount = rouletteItems[itemIndex].Bonus;

		var (isOver, resultAmount) = await _statisticsService.IncrementTotalDistributeAmountAsync(
			command.PromotionId,
			promotionSettings.PromotionLimitAmount,
			distributeAmount,
			rouletteItems[0].Bonus,
			cancellationToken).ConfigureAwait(false);

		if (!isOver)
		{
			var recordId = await _tgsRepository.AddRouletteRecordAsync(
				command.PromotionId,
				command.PlayerCode,
				brandId,
				command.Date,
				itemIndex,
				distributeAmount,
				cancellationToken).ConfigureAwait(false);

			return (recordId, resultAmount, distributeAmount, rouletteItems[itemIndex].CalculateBucket());
		}
		else
		{
			var recordId = await _tgsRepository.AddRouletteRecordAsync(
				command.PromotionId,
				command.PlayerCode,
				brandId,
				command.Date,
				0,
				rouletteItems[0].Bonus,
				cancellationToken).ConfigureAwait(false);

			return (recordId, resultAmount, rouletteItems[0].Bonus, rouletteItems[0].CalculateBucket());
		}
	}

	private async Task<int> PlayRouletteAsync(
		RouletteCommand command,
		RouletteItem[] rouletteItems,
		CancellationToken cancellationToken)
	{
		var pie = await _shuffleService
			.ShuffleAsync(command.PromotionId, rouletteItems)
			.ConfigureAwait(false);

		var deckIndex = RandomNumberGenerator.GetInt32(0, pie.Length);

		var itemIndex = pie[deckIndex];

		if (!await _statisticsService.IncrementRouletteItemCountAsync(
			command.PromotionId,
			rouletteItems[itemIndex].Id,
			command.Date,
			rouletteItems[itemIndex].IsDefault || rouletteItems[itemIndex].DailyWinningLimit == 0
				? null
				: rouletteItems[itemIndex].DailyWinningLimit,
			cancellationToken).ConfigureAwait(false))
			itemIndex = 0;

		return itemIndex;
	}
}
