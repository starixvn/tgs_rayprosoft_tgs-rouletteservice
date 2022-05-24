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

		if (isOverLimitAmount)
			resultAmount = await _statisticsRepository
				.DecreaseTotalDistributeAmountAsync(promotionId, amount - defaultAmount, cancellationToken)
				.ConfigureAwait(false);

		return (isOverLimitAmount, resultAmount);
	}
}
