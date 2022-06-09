namespace Tgs.RouletteServices;

internal class StatisticsService : IStatisticsService
{
	private readonly IStatisticsRepository _statisticsRepository;

	public StatisticsService(IStatisticsRepository statisticsRepository)
	{
		_statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
	}

	public async ValueTask<bool> CheckOverTotalDistributeAmountAsync(Guid promotionId, decimal limitAmount, CancellationToken cancellationToken = default)
	{
		var totalDistributeAmount = await _statisticsRepository
			.GetTotalDistributeAmountAsync(promotionId, cancellationToken)
			.ConfigureAwait(false);

		return totalDistributeAmount >= limitAmount;
	}

	public async ValueTask<(bool IsOver, decimal ResultAmount)> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal? limitAmount,
		decimal amount,
		decimal defaultAmount,
		CancellationToken cancellationToken = default)
	{
		var resultAmount = await _statisticsRepository
			.IncrementTotalDistributeAmountAsync(promotionId, amount, cancellationToken)
			.ConfigureAwait(false);

		var isOverLimitAmount = resultAmount > limitAmount
			&& resultAmount - amount >= limitAmount;

		var differenceAmount = amount - defaultAmount;
		if (isOverLimitAmount && differenceAmount != 0)
			resultAmount = await _statisticsRepository
				.DecreaseTotalDistributeAmountAsync(promotionId, differenceAmount, cancellationToken)
				.ConfigureAwait(false);

		return (isOverLimitAmount, resultAmount);
	}

	public async ValueTask<bool> IncrementRouletteItemCountAsync(
		Guid promotionId,
		Guid itemId,
		DateOnly date,
		int? limitCount,
		CancellationToken cancellationToken = default)
	{
		var result = await _statisticsRepository
			.IncrementRouletteItemCountAsync(promotionId, itemId, date, cancellationToken)
			.ConfigureAwait(false);

		if (limitCount is not null && result > limitCount)
		{
			_ = await _statisticsRepository
				.DecreaseRouletteItemCountAsync(promotionId, itemId, date, cancellationToken)
				.ConfigureAwait(false);

			return false;
		}

		return true;
	}
}
