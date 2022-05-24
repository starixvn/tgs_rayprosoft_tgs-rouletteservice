namespace Tgs.RouletteServices;

public interface IStatisticsService
{
	ValueTask<bool> CheckOverTotalDistributeAmountAsync(Guid promotionId, decimal limitAmount, CancellationToken cancellationToken = default);

	ValueTask<(bool IsOver, decimal ResultAmount)> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal? limitAmount,
		decimal amount,
		decimal defaultAmount,
		CancellationToken cancellationToken = default);
}
